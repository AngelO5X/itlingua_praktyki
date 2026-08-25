using Microsoft.EntityFrameworkCore;
using TutoringBackend.Api.Data;
using TutoringBackend.Api.Models;

namespace TutoringBackend.Api.Services;

public interface IScheduleGenerationService
{
    /// Generuje brakujące konkretne lekcje (Lesson) na podstawie aktywnych ScheduleSlot
    /// dla dat od dziś do kiedyś w przód.
    Task<int> GenerateUpcomingLessonsAsync(int horizonDays = 21, CancellationToken ct = default);
}

public class ScheduleGenerationService : IScheduleGenerationService
{
    private readonly ApplicationDbContext _db;

    public ScheduleGenerationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> GenerateUpcomingLessonsAsync(int horizonDays = 21, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(horizonDays);

        var activeSlots = await _db.ScheduleSlots
            .Include(s => s.Assignment)
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        var created = 0;

        foreach (var slot in activeSlots)
        {
            var from = slot.EffectiveFromDate > today ? slot.EffectiveFromDate : today;
            var to = slot.EffectiveToDate.HasValue && slot.EffectiveToDate < horizon ? slot.EffectiveToDate.Value : horizon;

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                if (date.DayOfWeek != slot.DayOfWeek) continue;

                var exists = await _db.Lessons.AnyAsync(l =>
                    l.TeacherStudentAssignmentId == slot.TeacherStudentAssignmentId &&
                    l.LessonDate == date && l.StartTime == slot.StartTime, ct);
                if (exists) continue;

                _db.Lessons.Add(new Lesson
                {
                    TeacherStudentAssignmentId = slot.TeacherStudentAssignmentId,
                    ScheduleSlotId = slot.Id,
                    LessonDate = date,
                    StartTime = slot.StartTime,
                    DurationMinutes = slot.DurationMinutes,
                    Price = slot.Assignment.RatePerLesson,
                    Status = LessonStatus.Planned
                });
                created++;
            }
        }

        if (created > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        return created;
    }
}
