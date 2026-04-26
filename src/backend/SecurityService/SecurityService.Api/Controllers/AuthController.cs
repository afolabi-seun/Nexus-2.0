using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityService.Api.Attributes;
using SecurityService.Api.Extensions;
using SecurityService.Application.DTOs;
using SecurityService.Application.DTOs.Auth;
using SecurityService.Application.DTOs.Otp;
using SecurityService.Domain.Interfaces.Services.Auth;
using SecurityService.Domain.Interfaces.Services.Otp;
using SecurityService.Infrastructure.Configuration;

namespace SecurityService.Api.Controllers;

/// <summary>
/// Handles authentication operations including login, logout, token refresh, OTP, and credential generation.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOtpService _otpService;
    private readonly AppSettings _appSettings;

    private const string RefreshTokenCookieName = "nexus_refresh";

    public AuthController(IAuthService authService, IOtpService otpService, AppSettings appSettings)
    {
        _authService = authService;
        _otpService = otpService;
        _appSettings = appSettings;
    }

    /// <summary>
    /// Authenticate a user with email and password.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var deviceId = request.DeviceId ?? Guid.NewGuid().ToString("N");

        var result = await _authService.LoginAsync(request.Email, request.Password, ipAddress, deviceId, ct);

        SetRefreshTokenCookie(result.RefreshToken);

        var response = new LoginResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = string.Empty,
            ExpiresIn = result.ExpiresIn,
            IsFirstTimeUser = result.IsFirstTimeUser
        };

        return ApiResponse<LoginResponse>.Ok(response, "Login successful.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Logout the current session.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var deviceId = HttpContext.Items["deviceId"]?.ToString() ?? string.Empty;
        var jti = HttpContext.Items["jti"]?.ToString() ?? string.Empty;
        var tokenExpiry = HttpContext.Items.TryGetValue("tokenExpiry", out var exp) && exp is DateTime dt
            ? dt
            : DateTime.UtcNow.AddMinutes(15);

        await _authService.LogoutAsync(userId, deviceId, jti, tokenExpiry, ct);

        ClearRefreshTokenCookie();

        return ApiResponse<object>.Ok(null!, "Logout successful.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Refresh the access token using the httpOnly refresh token cookie.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        // Read refresh token from httpOnly cookie first, fall back to request body for backward compat
        var refreshToken = HttpContext.Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            refreshToken = request.RefreshToken;

        if (string.IsNullOrEmpty(refreshToken))
            return ApiResponse<object>.Fail(2013, "REFRESH_TOKEN_REUSE", "No refresh token provided.")
                .ToActionResult(HttpContext);

        var result = await _authService.RefreshTokenAsync(refreshToken, request.DeviceId, ct);

        SetRefreshTokenCookie(result.RefreshToken);

        var response = new LoginResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = string.Empty,
            ExpiresIn = result.ExpiresIn,
            IsFirstTimeUser = result.IsFirstTimeUser
        };

        return ApiResponse<LoginResponse>.Ok(response, "Token refreshed.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Request a one-time password (OTP) for the given identity.
    /// </summary>
    [HttpPost("otp/request")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestOtp(
        [FromBody] OtpRequest request, CancellationToken ct)
    {
        await _otpService.GenerateOtpAsync(request.Identity, ct);

        return ApiResponse<object>.Ok(null!, "OTP sent successfully.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Verify a one-time password (OTP) code.
    /// </summary>
    [HttpPost("otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] OtpVerifyRequest request, CancellationToken ct)
    {
        await _otpService.VerifyOtpAsync(request.Identity, request.Code, ct);

        return ApiResponse<object>.Ok(null!, "OTP verified successfully.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Generate credentials for a new team member (service-to-service).
    /// </summary>
    [HttpPost("credentials/generate")]
    [ServiceAuth]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GenerateCredentials(
        [FromBody] CredentialGenerateRequest request, CancellationToken ct)
    {
        await _authService.GenerateCredentialsAsync(request.MemberId, request.Email, ct);

        return ApiResponse<object>.Ok(null!, "Credentials generated successfully.").ToActionResult(HttpContext);
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            MaxAge = TimeSpan.FromDays(_appSettings.RefreshTokenExpiryDays),
        };

        HttpContext.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    private void ClearRefreshTokenCookie()
    {
        HttpContext.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
        });
    }
}
