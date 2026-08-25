using Microsoft.EntityFrameworkCore;
using TutoringBackend.Api.Data;
using TutoringBackend.Api.Models;

namespace TutoringBackend.Api.Services;

public interface ILessonService
{
    Task<Lesson> CompleteLessonAsync(Guid lessonId, Guid actingTeacherId, CancellationToken ct = default);
    Task<Lesson> CancelLessonAsync(Guid lessonId, Guid actingTeacherId, CancellationToken ct = default);
    Task<Lesson> RescheduleLessonAsync(Guid lessonId, Guid actingTeacherId, DateOnly newDate, TimeSpan newStartTime, CancellationToken ct = default);

    /// Przenosi wszystkie zaplanowane lekcje danego dnia nauczyciela o wskazane przesunięcie dat.
    Task<int> RescheduleWholeDayAsync(Guid actingTeacherId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
}

public class LessonService : ILessonService
{
    private readonly ApplicationDbContext _db;
    private readonly IBalanceService _balanceService;

    public LessonService(ApplicationDbContext db, IBalanceService balanceService)
    {
        _db = db;
        _balanceService = balanceService;
    }

    private async Task<Lesson> GetOwnedLessonAsync(Guid lessonId, Guid teacherId, CancellationToken ct)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Assignment)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct)
            ?? throw new KeyNotFoundException("Lekcja nie istnieje.");

        if (lesson.Assignment.TeacherId != teacherId)
            throw new UnauthorizedAccessException("Ta lekcja nie należy do tego nauczyciela.");

        return lesson;
    }

    public async Task<Lesson> CompleteLessonAsync(Guid lessonId, Guid actingTeacherId, CancellationToken ct = default)
    {
        var lesson = await GetOwnedLessonAsync(lessonId, actingTeacherId, ct);

        if (lesson.Status != LessonStatus.Planned)
            throw new InvalidOperationException("Tylko zaplanowaną lekcję można zamknąć jako odbytą.");

        lesson.Status = LessonStatus.Completed;
        lesson.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Odjęcie z salda następuje dopiero przy zamknięciu lekcji
        await _balanceService.ApplyTransactionAsync(
            lesson.Assignment.StudentId,
            -lesson.Price,
            BalanceTransactionType.LessonCharge,
            actingTeacherId,
            lesson.Id,
            "Odjęcie za odbytą lekcję",
            ct);

        return lesson;
    }

    public async Task<Lesson> CancelLessonAsync(Guid lessonId, Guid actingTeacherId, CancellationToken ct = default)
    {
        var lesson = await GetOwnedLessonAsync(lessonId, actingTeacherId, ct);

        if (lesson.Status != LessonStatus.Planned)
            throw new InvalidOperationException("Tylko zaplanowaną lekcję można odwołać.");

        var teacher = await _db.Users.FirstAsync(u => u.Id == actingTeacherId, ct);
        var lessonStartUtc = lesson.LessonDate.ToDateTime(TimeOnly.FromTimeSpan(lesson.StartTime), DateTimeKind.Utc);
        var hoursNotice = (lessonStartUtc - DateTime.UtcNow).TotalHours;

        var isValidCancellation = hoursNotice >= teacher.CancellationNoticeHours;

        lesson.Status = isValidCancellation ? LessonStatus.CancelledValid : LessonStatus.CancelledInvalid;
        lesson.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (!isValidCancellation)
        {
            // Odwołana bez wyprzedzenia -> saldo przepada (obciążenie tak jakby lekcja się odbyła).
            await _balanceService.ApplyTransactionAsync(
                lesson.Assignment.StudentId,
                -lesson.Price,
                BalanceTransactionType.CancellationPenalty,
                actingTeacherId,
                lesson.Id,
                "Odwołanie bez wymaganego wyprzedzenia",
                ct);
        }
        // Odwołana prawidłowo -> saldo zostaje nietknięte, "przechodzi" na kolejny miesiąc.

        return lesson;
    }

    public async Task<Lesson> RescheduleLessonAsync(Guid lessonId, Guid actingTeacherId, DateOnly newDate, TimeSpan newStartTime, CancellationToken ct = default)
    {
        var lesson = await GetOwnedLessonAsync(lessonId, actingTeacherId, ct);

        if (lesson.Status != LessonStatus.Planned)
            throw new InvalidOperationException("Tylko zaplanowaną lekcję można przenieść.");

        var conflict = await _db.Lessons.AnyAsync(l =>
            l.TeacherStudentAssignmentId == lesson.TeacherStudentAssignmentId &&
            l.LessonDate == newDate && l.StartTime == newStartTime && l.Id != lesson.Id, ct);
        if (conflict)
            throw new InvalidOperationException("W tym terminie już istnieje inna lekcja dla tej pary.");

        var newLesson = new Lesson
        {
            TeacherStudentAssignmentId = lesson.TeacherStudentAssignmentId,
            ScheduleSlotId = lesson.ScheduleSlotId,
            LessonDate = newDate,
            StartTime = newStartTime,
            DurationMinutes = lesson.DurationMinutes,
            Price = lesson.Price,
            Status = LessonStatus.Planned,
            RescheduledFromLessonId = lesson.Id
        };

        lesson.Status = LessonStatus.Rescheduled;
        lesson.UpdatedAtUtc = DateTime.UtcNow;

        _db.Lessons.Add(newLesson);
        await _db.SaveChangesAsync(ct);

        return newLesson;
    }

    public async Task<int> RescheduleWholeDayAsync(Guid actingTeacherId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var lessons = await _db.Lessons
            .Include(l => l.Assignment)
            .Where(l => l.Assignment.TeacherId == actingTeacherId
                        && l.LessonDate == fromDate
                        && l.Status == LessonStatus.Planned)
            .ToListAsync(ct);

        var count = 0;
        foreach (var lesson in lessons)
        {
            await RescheduleLessonAsync(lesson.Id, actingTeacherId, toDate, lesson.StartTime, ct);
            count++;
        }

        return count;
    }
}
