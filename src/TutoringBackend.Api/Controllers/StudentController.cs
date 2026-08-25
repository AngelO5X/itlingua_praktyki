using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutoringBackend.Api.Common;
using TutoringBackend.Api.Data;

namespace TutoringBackend.Api.Controllers;

[ApiController]
[Route("api/student")]
[Authorize(Roles = Roles.Student)]
public class StudentController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StudentController(ApplicationDbContext db)
    {
        _db = db;
    }

    private Guid CurrentStudentId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var student = await _db.Users.FirstAsync(u => u.Id == CurrentStudentId);
        return Ok(new { balance = student.Balance });
    }

    [HttpGet("balance/history")]
    public async Task<IActionResult> GetBalanceHistory()
    {
        var studentId = CurrentStudentId;
        var history = await _db.BalanceTransactions
            .Where(t => t.StudentId == studentId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new { t.Id, t.Amount, t.Type, t.Note, t.CreatedAtUtc })
            .ToListAsync();

        return Ok(history);
    }

    /// Uczeń widzi TYLKO swoje lekcje - filtr po StudentId
    [HttpGet("lessons")]
    public async Task<IActionResult> GetMyLessons([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var studentId = CurrentStudentId;
        var lessons = await _db.Lessons
            .Include(l => l.Assignment).ThenInclude(a => a.Teacher)
            .Include(l => l.Materials)
            .Where(l => l.Assignment.StudentId == studentId && l.LessonDate >= from && l.LessonDate <= to)
            .OrderBy(l => l.LessonDate).ThenBy(l => l.StartTime)
            .ToListAsync();

        return Ok(lessons.Select(TeacherController.MapLesson));
    }

    [HttpGet("lessons/{id:guid}")]
    public async Task<IActionResult> GetLessonDetails(Guid id)
    {
        var studentId = CurrentStudentId;
        var lesson = await _db.Lessons
            .Include(l => l.Assignment).ThenInclude(a => a.Teacher)
            .Include(l => l.Materials)
            .FirstOrDefaultAsync(l => l.Id == id && l.Assignment.StudentId == studentId);

        if (lesson is null) return NotFound();

        return Ok(TeacherController.MapLesson(lesson));
    }
}
