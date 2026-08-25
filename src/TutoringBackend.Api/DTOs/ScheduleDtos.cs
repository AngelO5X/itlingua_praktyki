using System.ComponentModel.DataAnnotations;

namespace TutoringBackend.Api.DTOs;

public record CreateScheduleSlotRequest(
    [property: Required] Guid AssignmentId,
    [property: Range(0, 6)] DayOfWeek DayOfWeek,
    [property: Required] TimeSpan StartTime,
    [property: Range(15, 240)] int DurationMinutes,
    [property: Required] DateOnly EffectiveFromDate,
    DateOnly? EffectiveToDate
);

public record RescheduleLessonRequest(
    [property: Required] DateOnly NewDate,
    [property: Required] TimeSpan NewStartTime,
    /// Jeśli true - front najpierw pyta użytkownika o potwierdzenie przeniesienia całego dnia.
    bool ApplyToWholeDay
);

public record RescheduleWholeDayRequest(
    [property: Required] DateOnly FromDate,
    [property: Required] DateOnly ToDate
);
