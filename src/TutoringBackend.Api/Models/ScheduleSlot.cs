namespace TutoringBackend.Api.Models;

/// Cykliczny (co tydzień) slot w kalendarzu nauczyciela dla danego ucznia.
/// Na jego podstawie generowane są konkretne wystąpienia (Lesson).
/// EffectiveTo != null oznacza zmianę tymczasową / zakończoną - poza tym zakresem
/// slot przestaje generować nowe lekcje.
public class ScheduleSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TeacherStudentAssignmentId { get; set; }
    public TeacherStudentAssignment Assignment { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    /// Godzina rozpoczęcia lekcji (czas lokalny nauczyciela, przechowywany jako TimeSpan UTC-naive).
    public TimeSpan StartTime { get; set; }

    public int DurationMinutes { get; set; } = 45;

    public DateOnly EffectiveFromDate { get; set; }

    /// Null = obowiązuje bezterminowo.
    public DateOnly? EffectiveToDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Lesson> GeneratedLessons { get; set; } = new List<Lesson>();
}
