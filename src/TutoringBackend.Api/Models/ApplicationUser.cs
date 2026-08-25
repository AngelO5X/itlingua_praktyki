using Microsoft.AspNetCore.Identity;

namespace TutoringBackend.Api.Models;

/// Rozszerzony użytkownik Identity. Role (Uczen / Nauczyciel / Admin) trzymane są
/// w standardowym mechanizmie ról ASP.NET Identity (tabele AspNetRoles / AspNetUserRoles)
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// Saldo konta - ma sens tylko dla ucznia, ale trzymane centralnie żeby uprościć model.
    /// Nigdy nie modyfikować bezpośrednio z kontrolera - zawsze przez BalanceService,
    /// który tworzy powiązany wpis w BalanceTransaction (audyt).
    public decimal Balance { get; set; } = 0m;

    /// Ile godzin przed lekcją nauczyciel wymaga odwołania, żeby uznać je za "prawidłowe"
    /// (saldo nie przepada).
    public int CancellationNoticeHours { get; set; } = 24;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Refresh token
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
}
