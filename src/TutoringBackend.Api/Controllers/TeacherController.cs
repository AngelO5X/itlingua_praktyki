using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutoringBackend.Api.Common;
using TutoringBackend.Api.Data;
using TutoringBackend.Api.DTOs;
using TutoringBackend.Api.Models;
using TutoringBackend.Api.Services;

namespace TutoringBackend.Api.Controllers;

[ApiController]
[Route("api/teacher")]
[Authorize(Roles = Roles.Teacher)]
public class TeacherController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILessonService _lessonService;

    public TeacherController(ApplicationDbContext db, ILessonService lessonService)
    {
        _db = db;
        _lessonService = lessonService;
    }

    private Guid CurrentTeacherId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// Lista przypisanych uczniów wraz z podstawowymi statystykami.
    [HttpGet("students")]
    public async Task<IActionResult> GetMyStudents()
    {
        var teacherId = CurrentTeacherId;

        var data = await _db.TeacherStudentAssignments
            .Where(a => a.TeacherId == teacherId && a.IsActive)
            .Select(a => new
            {
                a.Id,
                StudentId = a.StudentId,
                StudentName = a.Student.FirstName + " " + a.Student.LastName,
                a.RatePerLesson,
                a.DefaultDurationMinutes,
                CompletedLessons = a.Lessons.Count(l => l.Status == LessonStatus.Completed),
                TotalMinutes = a.Lessons.Where(l => l.Status == LessonStatus.Completed).Sum(l => l.DurationMinutes)
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpPost("schedule")]
    public async Task<IActionResult> CreateScheduleSlot([FromBody] CreateScheduleSlotRequest request)
    {
        var teacherId = CurrentTeacherId;
        var assignment = await _db.TeacherStudentAssignments
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId && a.TeacherId == teacherId && a.IsActive);
        if (assignment is null) return BadRequest(new { error = "Nieprawidłowe przypisanie ucznia." });

        var slot = new ScheduleSlot
        {
            TeacherStudentAssignmentId = request.AssignmentId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            DurationMinutes = request.DurationMinutes,
            EffectiveFromDate = request.EffectiveFromDate,
            EffectiveToDate = request.EffectiveToDate
        };

        _db.ScheduleSlots.Add(slot);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(CreateScheduleSlot), new { id = slot.Id }, slot.Id);
    }

    [HttpGet("schedule")]
    public async Task<IActionResult> GetMySchedule()
    {
        var teacherId = CurrentTeacherId;
        var slots = await _db.ScheduleSlots
            .Where(s => s.Assignment.TeacherId == teacherId && s.IsActive)
            .Select(s => new
            {
                s.Id,
                s.TeacherStudentAssignmentId,
                StudentName = s.Assignment.Student.FirstName + " " + s.Assignment.Student.LastName,
                s.DayOfWeek,
                s.StartTime,
                s.DurationMinutes,
                s.EffectiveFromDate,
                s.EffectiveToDate
            })
            .ToListAsync();

        return Ok(slots);
    }

    /// Kalendarz - lekcje (już wygenerowane wystąpienia) w zadanym zakresie dat, do drag&drop na froncie.
    [HttpGet("lessons")]
    public async Task<IActionResult> GetLessons([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        if (to < from) return BadRequest(new { error = "Zakres dat jest nieprawidłowy." });

        var teacherId = CurrentTeacherId;
        var lessons = await _db.Lessons
            .Include(l => l.Assignment).ThenInclude(a => a.Student)
            .Include(l => l.Materials)
            .Where(l => l.Assignment.TeacherId == teacherId && l.LessonDate >= from && l.LessonDate <= to)
            .OrderBy(l => l.LessonDate).ThenBy(l => l.StartTime)
            .ToListAsync();

        return Ok(lessons.Select(MapLesson));
    }

    [HttpPut("lessons/{id:guid}/details")]
    public async Task<IActionResult> UpdateLessonDetails(Guid id, [FromBody] UpdateLessonDetailsRequest request)
    {
        var teacherId = CurrentTeacherId;
        var lesson = await _db.Lessons.Include(l => l.Assignment)
            .FirstOrDefaultAsync(l => l.Id == id && l.Assignment.TeacherId == teacherId);
        if (lesson is null) return NotFound();

        // Kodowanie HTML jako dodatkowa warstwa obrony przed stored XSS
        lesson.Topic = InputSanitizer.EncodeHtml(request.Topic);
        lesson.Description = InputSanitizer.EncodeHtml(request.Description);
        lesson.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("lessons/{id:guid}/materials/link")]
    public async Task<IActionResult> AddLinkMaterial(Guid id, [FromBody] AddLinkMaterialRequest request)
    {
        var teacherId = CurrentTeacherId;
        var lesson = await _db.Lessons.Include(l => l.Assignment)
            .FirstOrDefaultAsync(l => l.Id == id && l.Assignment.TeacherId == teacherId);
        if (lesson is null) return NotFound();

        if (!InputSanitizer.IsSafeHttpUrl(request.Url))
            return BadRequest(new { error = "Nieprawidłowy adres URL (dozwolone tylko http/https)." });

        _db.LessonMaterials.Add(new LessonMaterial
        {
            LessonId = id,
            Type = MaterialType.Link,
            Url = request.Url,
            DisplayName = InputSanitizer.EncodeHtml(request.DisplayName)!,
            AddedByUserId = teacherId
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// Upload pliku jako materiału. Walidacja rozszerzenia i rozmiaru, plik zapisywany
    /// pod losową nazwą (GUID) poza katalogiem webroot - blokuje path traversal i
    /// wykonywalne rozszerzenia.
    [HttpPost("lessons/{id:guid}/materials/file")]
    [RequestSizeLimit(InputSanitizer.MaxFileSizeBytes)]
    public async Task<IActionResult> AddFileMaterial(Guid id, IFormFile file, [FromServices] IWebHostEnvironment env)
    {
        var teacherId = CurrentTeacherId;
        var lesson = await _db.Lessons.Include(l => l.Assignment)
            .FirstOrDefaultAsync(l => l.Id == id && l.Assignment.TeacherId == teacherId);
        if (lesson is null) return NotFound();

        if (file is null || file.Length == 0) return BadRequest(new { error = "Brak pliku." });
        if (file.Length > InputSanitizer.MaxFileSizeBytes) return BadRequest(new { error = "Plik jest za duży." });
        if (!InputSanitizer.IsAllowedFileExtension(file.FileName))
            return BadRequest(new { error = "Niedozwolony typ pliku." });

        var storedName = InputSanitizer.GenerateSafeStoredFileName(file.FileName);
        var storageRoot = Path.Combine(env.ContentRootPath, "lesson-materials");
        Directory.CreateDirectory(storageRoot);
        var fullPath = Path.Combine(storageRoot, storedName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        _db.LessonMaterials.Add(new LessonMaterial
        {
            LessonId = id,
            Type = MaterialType.File,
            OriginalFileName = InputSanitizer.EncodeHtml(Path.GetFileName(file.FileName)),
            StoredFileName = storedName,
            FileSizeBytes = file.Length,
            DisplayName = InputSanitizer.EncodeHtml(Path.GetFileNameWithoutExtension(file.FileName))!,
            AddedByUserId = teacherId
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("lessons/{id:guid}/complete")]
    public async Task<IActionResult> CompleteLesson(Guid id)
    {
        var lesson = await _lessonService.CompleteLessonAsync(id, CurrentTeacherId);
        return Ok(MapLesson(lesson));
    }

    [HttpPost("lessons/{id:guid}/cancel")]
    public async Task<IActionResult> CancelLesson(Guid id)
    {
        var lesson = await _lessonService.CancelLessonAsync(id, CurrentTeacherId);
        return Ok(MapLesson(lesson));
    }

    /// Drag&drop pojedynczej lekcji, opcjonalnie z przeniesieniem całego dnia po potwierdzeniu przez front.
    [HttpPost("lessons/{id:guid}/reschedule")]
    public async Task<IActionResult> RescheduleLesson(Guid id, [FromBody] RescheduleLessonRequest request)
    {
        if (request.ApplyToWholeDay)
        {
            var original = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == id);
            if (original is null) return NotFound();

            var count = await _lessonService.RescheduleWholeDayAsync(CurrentTeacherId, original.LessonDate, request.NewDate);
            return Ok(new { movedCount = count });
        }

        var lesson = await _lessonService.RescheduleLessonAsync(id, CurrentTeacherId, request.NewDate, request.NewStartTime);
        return Ok(MapLesson(lesson));
    }

    [HttpPost("cancellation-notice-hours/{hours:int}")]
    public async Task<IActionResult> SetCancellationNoticeHours(int hours)
    {
        if (hours < 0 || hours > 168) return BadRequest(new { error = "Nieprawidłowa liczba godzin." });

        var teacher = await _db.Users.FirstAsync(u => u.Id == CurrentTeacherId);
        teacher.CancellationNoticeHours = hours;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<LessonsSummaryResponse>> GetSummary([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var teacherId = CurrentTeacherId;
        var lessons = await _db.Lessons
            .Where(l => l.Assignment.TeacherId == teacherId && l.LessonDate >= from && l.LessonDate <= to)
            .ToListAsync();

        return Ok(new LessonsSummaryResponse(
            lessons.Count(l => l.Status == LessonStatus.Planned),
            lessons.Count(l => l.Status == LessonStatus.Completed),
            lessons.Count(l => l.Status == LessonStatus.CancelledValid),
            lessons.Count(l => l.Status == LessonStatus.CancelledInvalid),
            lessons.Where(l => l.Status == LessonStatus.Completed).Sum(l => l.DurationMinutes)
        ));
    }

    internal static LessonResponse MapLesson(Lesson l) => new(
        l.Id,
        l.TeacherStudentAssignmentId,
        l.Assignment.Student.FirstName + " " + l.Assignment.Student.LastName,
        l.Assignment.Teacher?.FirstName + " " + l.Assignment.Teacher?.LastName,
        l.LessonDate,
        l.StartTime,
        l.DurationMinutes,
        l.Status,
        l.Topic,
        l.Description,
        l.Price,
        l.IsPaid,
        l.Materials.Select(m => new LessonMaterialResponse(m.Id, m.Type, m.DisplayName, m.Url, m.OriginalFileName, m.AddedAtUtc))
    );
}
