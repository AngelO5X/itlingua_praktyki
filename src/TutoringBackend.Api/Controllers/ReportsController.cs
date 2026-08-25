using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutoringBackend.Api.Common;
using TutoringBackend.Api.Data;
using TutoringBackend.Api.Models;

namespace TutoringBackend.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = Roles.Teacher + "," + Roles.Admin)]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReportsController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// Raport: ilość odbytych / zaplanowanych / odwołanych (prawidłowo i nieprawidłowo).
    /// Nauczyciel widzi tylko swoje dane, Admin może podać teacherId żeby zobaczyć dowolnego nauczyciela
    [HttpGet("lessons")]
    public async Task<IActionResult> GetLessonReport([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] Guid? teacherId)
    {
        var isAdmin = User.IsInRole(Roles.Admin);
        var effectiveTeacherId = isAdmin && teacherId.HasValue
            ? teacherId.Value
            : Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var query = _db.Lessons
            .Where(l => l.LessonDate >= from && l.LessonDate <= to);

        if (!isAdmin || teacherId.HasValue)
        {
            query = query.Where(l => l.Assignment.TeacherId == effectiveTeacherId);
        }

        var lessons = await query.ToListAsync();

        var report = new
        {
            Planned = lessons.Count(l => l.Status == LessonStatus.Planned),
            Completed = lessons.Count(l => l.Status == LessonStatus.Completed),
            CancelledValid = lessons.Count(l => l.Status == LessonStatus.CancelledValid),
            CancelledInvalid = lessons.Count(l => l.Status == LessonStatus.CancelledInvalid),
            Total = lessons.Count
        };

        return Ok(report);
    }
}
