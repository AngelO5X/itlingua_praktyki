namespace TutoringBackend.Api.Models;

/// Konkretne, jednorazowe wystąpienie lekcji (wygenerowane ze ScheduleSlot albo dodane ręcznie).
/// Wszystkie pola tekstowe (Topic/Description) są HTML-encodowane przed zapisem.
public class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TeacherStudentAssignmentId { get; set; }
    public TeacherStudentAssignment Assignment { get; set; } = null!;

    public Guid? ScheduleSlotId { get; set; }
    public ScheduleSlot? ScheduleSlot { get; set; }

    public DateOnly LessonDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public int DurationMinutes { get; set; }

    public LessonStatus Status { get; set; } = LessonStatus.Planned;

    public string? Topic { get; set; }
    public string? Description { get; set; }

    /// Cena "zamrożona" w momencie utworzenia lekcji (niezależna od późniejszej zmiany stawki).
    public decimal Price { get; set; }

    public bool IsPaid { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    /// Jeśli lekcja powstała z przeniesienia innej - wskazuje na oryginał (audyt historii przeniesień).
    public Guid? RescheduledFromLessonId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<LessonMaterial> Materials { get; set; } = new List<LessonMaterial>();
}
