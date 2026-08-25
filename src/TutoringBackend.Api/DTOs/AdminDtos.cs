using System.ComponentModel.DataAnnotations;

namespace TutoringBackend.Api.DTOs;

public record CreateUserRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MinLength(10), MaxLength(200)] string TemporaryPassword,
    [property: Required, MaxLength(100)] string FirstName,
    [property: Required, MaxLength(100)] string LastName
);

public record CreateAssignmentRequest(
    [property: Required] Guid TeacherId,
    [property: Required] Guid StudentId,
    [property: Range(0, 100000)] decimal RatePerLesson,
    [property: Range(15, 240)] int DefaultDurationMinutes
);

public record UpdateAssignmentRequest(
    [property: Range(0, 100000)] decimal RatePerLesson,
    [property: Range(15, 240)] int DefaultDurationMinutes,
    bool IsActive
);

public record BalanceAdjustmentRequest(
    [property: Required] Guid StudentId,
    decimal Amount,
    [property: MaxLength(500)] string? Note
);

public record UserSummaryResponse(
    Guid Id, string Email, string FirstName, string LastName, bool IsActive, decimal Balance
);
