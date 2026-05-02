using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryBackend.API.Middleware;
using VeterinaryBackend.Business.Common;
using VeterinaryBackend.Business.DTOs.Auth;
using VeterinaryBackend.Business.Services;

namespace VeterinaryBackend.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Login as the super admin. Returns a Bearer JWT used for admin endpoints.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken ct = default)
    {
        var token = await _authService.LoginAsync(request, ct);
        return Ok(token);
    }

    /// <summary>
    /// Change admin password while logged in. Requires current password.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto request,
        CancellationToken ct = default)
    {
        await _authService.ChangePasswordAsync(request, ct);
        return NoContent();
    }

    /// <summary>
    /// Reset admin password using the recovery key (configured in appsettings.json → Admin:RecoveryKey).
    /// Used when admin forgot the password.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto request,
        CancellationToken ct = default)
    {
        await _authService.ResetPasswordAsync(request, ct);
        return NoContent();
    }
}
