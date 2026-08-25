namespace TutoringBackend.Api.Models;

/// Każda zmiana salda ucznia zostawia ślad - saldo na ApplicationUser jest tylko
/// "cache'owaną" sumą, prawdą źródłową jest historia transakcji.
public class BalanceTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;

    /// Dodatnia = uznanie (doładowanie/zwrot), ujemna = obciążenie.
    public decimal Amount { get; set; }

    public BalanceTransactionType Type { get; set; }

    public Guid? LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    public string? Note { get; set; }

    /// Kto wykonał operację (admin?, nauczyciel?)
    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
