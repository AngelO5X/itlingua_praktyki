using System.ComponentModel.DataAnnotations;

namespace TutoringBackend.Api.DTOs;

public record LoginRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MaxLength(200)] string Password
);

public record RefreshRequest(
    [property: Required] string RefreshToken
);

public record ChangePasswordRequest(
    [property: Required] string CurrentPassword,
    [property: Required, MinLength(10), MaxLength(200)] string NewPassword
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IEnumerable<string> Roles
);
