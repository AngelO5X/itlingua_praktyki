using System.ComponentModel.DataAnnotations;
using TutoringBackend.Api.Models;

namespace TutoringBackend.Api.DTOs;

public record UpdateLessonDetailsRequest(
    [property: MaxLength(200)] string? Topic,
    [property: MaxLength(2000)] string? Description
);

public record AddLinkMaterialRequest(
    [property: Required, Url, MaxLength(2000)] string Url,
    [property: Required, MaxLength(200)] string DisplayName
);

public record LessonResponse(
    Guid Id,
    Guid AssignmentId,
    string StudentName,
    string TeacherName,
    DateOnly LessonDate,
    TimeSpan StartTime,
    int DurationMinutes,
    LessonStatus Status,
    string? Topic,
    string? Description,
    decimal Price,
    bool IsPaid,
    IEnumerable<LessonMaterialResponse> Materials
);

public record LessonMaterialResponse(
    Guid Id, MaterialType Type, string DisplayName, string? Url, string? OriginalFileName, DateTime AddedAtUtc
);

public record LessonsSummaryResponse(
    int PlannedCount,
    int CompletedCount,
    int CancelledValidCount,
    int CancelledInvalidCount,
    int TotalMinutes
);
