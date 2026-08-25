using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutoringBackend.Api.Common;
using TutoringBackend.Api.Data;
using TutoringBackend.Api.DTOs;
using TutoringBackend.Api.Models;
using TutoringBackend.Api.Services;

namespace TutoringBackend.Api.Controllers;

/// Wszystkie endpointy tego kontrolera są dostępne wyłącznie dla roli Admin.
[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IBalanceService _balanceService;

    public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext db, IBalanceService balanceService)
    {
        _userManager = userManager;
        _db = db;
        _balanceService = balanceService;
    }

    [HttpPost("users/students")]
    public async Task<ActionResult<UserSummaryResponse>> CreateStudent([FromBody] CreateUserRequest request)
        => await CreateUserAsync(request, Roles.Student);

    [HttpPost("users/teachers")]
    public async Task<ActionResult<UserSummaryResponse>> CreateTeacher([FromBody] CreateUserRequest request)
        => await CreateUserAsync(request, Roles.Teacher);

    private async Task<ActionResult<UserSummaryResponse>> CreateUserAsync(CreateUserRequest request, string role)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = InputSanitizer.EncodeHtml(request.FirstName)!,
            LastName = InputSanitizer.EncodeHtml(request.LastName)!,
            EmailConfirmed = true,
            IsActive = true
        };

        // UserManager.CreateAsync stosuje politykę haseł skonfigurowaną globalnie
        var result = await _userManager.CreateAsync(user, request.TemporaryPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, role);

        return Ok(new UserSummaryResponse(user.Id, user.Email!, user.FirstName, user.LastName, user.IsActive, user.Balance));
    }

    [HttpGet("students")]
    public async Task<ActionResult<IEnumerable<UserSummaryResponse>>> GetStudents()
    {
        var students = await _userManager.GetUsersInRoleAsync(Roles.Student);
        return Ok(students.Select(s => new UserSummaryResponse(s.Id, s.Email!, s.FirstName, s.LastName, s.IsActive, s.Balance)));
    }

    [HttpGet("teachers")]
    public async Task<ActionResult<IEnumerable<UserSummaryResponse>>> GetTeachers()
    {
        var teachers = await _userManager.GetUsersInRoleAsync(Roles.Teacher);
        return Ok(teachers.Select(t => new UserSummaryResponse(t.Id, t.Email!, t.FirstName, t.LastName, t.IsActive, t.Balance)));
    }

    [HttpPost("assignments")]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request)
    {
        var teacher = await _userManager.FindByIdAsync(request.TeacherId.ToString());
        var student = await _userManager.FindByIdAsync(request.StudentId.ToString());
        if (teacher is null || !(await _userManager.IsInRoleAsync(teacher, Roles.Teacher)))
            return BadRequest(new { error = "Wskazany nauczyciel nie istnieje." });
        if (student is null || !(await _userManager.IsInRoleAsync(student, Roles.Student)))
            return BadRequest(new { error = "Wskazany uczeń nie istnieje." });

        var alreadyAssigned = await _db.TeacherStudentAssignments
            .AnyAsync(a => a.StudentId == request.StudentId && a.IsActive);
        if (alreadyAssigned)
            return Conflict(new { error = "Ten uczeń ma już aktywne przypisanie do nauczyciela." });

        var assignment = new TeacherStudentAssignment
        {
            TeacherId = request.TeacherId,
            StudentId = request.StudentId,
            RatePerLesson = request.RatePerLesson,
            DefaultDurationMinutes = request.DefaultDurationMinutes
        };

        _db.TeacherStudentAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(CreateAssignment), new { id = assignment.Id }, assignment.Id);
    }

    [HttpPut("assignments/{id:guid}")]
    public async Task<IActionResult> UpdateAssignment(Guid id, [FromBody] UpdateAssignmentRequest request)
    {
        var assignment = await _db.TeacherStudentAssignments.FirstOrDefaultAsync(a => a.Id == id);
        if (assignment is null) return NotFound();

        assignment.RatePerLesson = request.RatePerLesson;
        assignment.DefaultDurationMinutes = request.DefaultDurationMinutes;
        assignment.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// Ręczna edycja salda ucznia przez admina - zawsze zostawia ślad w BalanceTransaction.
    [HttpPost("balance/adjust")]
    public async Task<IActionResult> AdjustBalance([FromBody] BalanceAdjustmentRequest request)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            await _balanceService.ApplyTransactionAsync(
                request.StudentId,
                request.Amount,
                BalanceTransactionType.TopUp,
                adminId,
                note: InputSanitizer.EncodeHtml(request.Note));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }

        return NoContent();
    }

    [HttpPost("users/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        user.IsActive = false;
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAtUtc = null;
        await _userManager.UpdateAsync(user);

        return NoContent();
    }
}
