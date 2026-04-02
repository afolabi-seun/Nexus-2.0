using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityService.Api.Extensions;
using SecurityService.Application.DTOs;
using SecurityService.Application.DTOs.Password;
using SecurityService.Domain.Helpers;
using SecurityService.Domain.Interfaces.Services.Password;
using SecurityService.Infrastructure.Services.ServiceClients;

namespace SecurityService.Api.Controllers;

/// <summary>
/// Handles password management operations including forced change and reset flows.
/// </summary>
[ApiController]
[Route("api/v1/password")]
public class PasswordController : ControllerBase
{
    private readonly IPasswordService _passwordService;
    private readonly IProfileServiceClient _profileServiceClient;

    public PasswordController(IPasswordService passwordService, IProfileServiceClient profileServiceClient)
    {
        _passwordService = passwordService;
        _profileServiceClient = profileServiceClient;
    }

    /// <summary>
    /// Change the current user's password (forced change for first-time users).
    /// </summary>
    /// <param name="request">New password meeting complexity requirements</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of password change</returns>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Password does not meet complexity requirements or was recently used</response>
    /// <response code="401">Unauthorized</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/password/forced-change
    ///     {
    ///         "newPassword": "NewPass@123"
    ///     }
    ///
    /// Password must be 8+ characters with uppercase, lowercase, digit, and special character.
    /// </remarks>
    [HttpPost("forced-change")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ForcedChange(
        [FromBody] ForcedPasswordChangeRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var roleName = HttpContext.Items.TryGetValue("roleName", out var rObj) ? rObj as string : null;

        await _passwordService.ForcedChangeAsync(userId, string.Empty, request.NewPassword, ct);

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        if (roleName == RoleNames.PlatformAdmin)
        {
            await _profileServiceClient.UpdatePlatformAdminPasswordAsync(userId, newHash, ct);
        }
        else
        {
            await _profileServiceClient.UpdatePasswordHashAsync(userId, newHash, ct);
            await _profileServiceClient.SetIsFirstTimeUserAsync(userId, false, ct);
        }

        return ApiResponse<object>.Ok(null!, "Password changed successfully.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Request a password reset OTP for the given email.
    /// </summary>
    /// <param name="request">Email address to send the reset OTP to</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation that OTP was sent</returns>
    /// <response code="200">Password reset OTP sent (always returns 200 to prevent email enumeration)</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/password/reset/request
    ///     {
    ///         "email": "user@example.com"
    ///     }
    ///
    /// </remarks>
    [HttpPost("reset/request")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetRequest(
        [FromBody] PasswordResetRequest request, CancellationToken ct)
    {
        await _passwordService.ResetRequestAsync(request.Email, ct);

        return ApiResponse<object>.Ok(null!, "Password reset OTP sent.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Confirm a password reset using OTP and set a new password.
    /// </summary>
    /// <param name="request">Email, OTP code, and new password</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of password reset</returns>
    /// <response code="200">Password reset successful</response>
    /// <response code="400">OTP expired, invalid, or password does not meet requirements</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/password/reset/confirm
    ///     {
    ///         "email": "user@example.com",
    ///         "otpCode": "123456",
    ///         "newPassword": "NewPass@123"
    ///     }
    ///
    /// </remarks>
    [HttpPost("reset/confirm")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetConfirm(
        [FromBody] PasswordResetConfirmRequest request, CancellationToken ct)
    {
        await _passwordService.ResetConfirmAsync(request.Email, request.OtpCode, request.NewPassword, ct);

        return ApiResponse<object>.Ok(null!, "Password reset successful.").ToActionResult(HttpContext);
    }
}
