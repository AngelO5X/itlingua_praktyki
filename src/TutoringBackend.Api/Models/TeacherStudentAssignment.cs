namespace TutoringBackend.Api.Models;

/// Przypisanie ucznia do nauczyciela wraz ze stawką i domyślnym czasem trwania lekcji.
/// Tworzone i edytowane wyłącznie przez Admina.
public class TeacherStudentAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TeacherId { get; set; }
    public ApplicationUser Teacher { get; set; } = null!;

    public Guid StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;

    /// Stawka za jedną lekcję (nie za godzinę, żeby uniknąć przeliczeń przy 45 min).
    public decimal RatePerLesson { get; set; }

    public int DefaultDurationMinutes { get; set; } = 45;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ScheduleSlot> ScheduleSlots { get; set; } = new List<ScheduleSlot>();
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
