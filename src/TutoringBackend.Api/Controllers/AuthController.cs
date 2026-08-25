using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TutoringBackend.Api.DTOs;
using TutoringBackend.Api.Models;
using TutoringBackend.Api.Services;

namespace TutoringBackend.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// Logowanie. Objęte limitem żądań (patrz Program.cs -> "login" policy) żeby utrudnić
    /// brute-force, a dodatkowo ASP.NET Identity ma wbudowany lockout po kilku nieudanych próbach
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            // Stała, "sztuczna" praca żeby czas odpowiedzi nie zdradzał, czy konto istnieje.
            await _userManager.CheckPasswordAsync(new ApplicationUser(), request.Password);
            return Unauthorized(new { error = "Nieprawidłowy e-mail lub hasło." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Konto {UserId} zablokowane po zbyt wielu nieudanych próbach logowania.", user.Id);
            return StatusCode(423, new { error = "Konto tymczasowo zablokowane. Spróbuj ponownie później." });
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new { error = "Nieprawidłowy e-mail lub hasło." });
        }

        return await IssueTokensAsync(user);
    }

    /// Odświeżenie access tokenu. Refresh token jest rotowany (stary przestaje działać).
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var hash = _tokenService.HashRefreshToken(request.RefreshToken);
        var user = _userManager.Users.FirstOrDefault(u =>
            u.RefreshTokenHash == hash && u.RefreshTokenExpiresAtUtc > DateTime.UtcNow);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { error = "Nieprawidłowy lub wygasły refresh token." });
        }

        return await IssueTokensAsync(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAtUtc = null;
            await _userManager.UpdateAsync(user);
        }

        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // Po zmianie hasła unieważniamy refresh token - wymusza ponowne logowanie na innych urządzeniach.
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAtUtc = null;
        await _userManager.UpdateAsync(user);

        return NoContent();
    }

    private async Task<ActionResult<AuthResponse>> IssueTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.CreateAccessToken(user, roles);
        var (rawRefresh, hash, expiresAt) = _tokenService.CreateRefreshToken();

        user.RefreshTokenHash = hash;
        user.RefreshTokenExpiresAtUtc = expiresAt;
        await _userManager.UpdateAsync(user);

        var accessMinutes = 15;
        return Ok(new AuthResponse(
            accessToken,
            rawRefresh,
            DateTime.UtcNow.AddMinutes(accessMinutes),
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            roles));
    }
}
