using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityService.Api.Attributes;
using SecurityService.Application.DTOs;
using SecurityService.Application.DTOs.Auth;
using SecurityService.Application.DTOs.Otp;
using SecurityService.Domain.Interfaces.Services.Auth;
using SecurityService.Domain.Interfaces.Services.Otp;

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

    public AuthController(IAuthService authService, IOtpService otpService)
    {
        _authService = authService;
        _otpService = otpService;
    }

    /// <summary>
    /// Authenticate a user with email and password.
    /// </summary>
    /// <param name="request">Login credentials containing email and password</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>JWT access token, refresh token, expiry, and first-time user flag</returns>
    /// <response code="200">Login successful — returns tokens</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="423">Account is locked due to repeated failed attempts</response>
    /// <response code="429">Rate limit exceeded</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/auth/login
    ///     {
    ///         "email": "admin@example.com",
    ///         "password": "Admin@123"
    ///     }
    ///
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var deviceId = request.DeviceId ?? Guid.NewGuid().ToString("N");

        var result = await _authService.LoginAsync(request.Email, request.Password, ipAddress, deviceId, ct);

        var response = new LoginResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn,
            IsFirstTimeUser = result.IsFirstTimeUser
        };

        var apiResponse = ApiResponse<LoginResponse>.Ok(response, "Login successful.");
        apiResponse.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(apiResponse);
    }

    /// <summary>
    /// Logout the current session.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of successful logout</returns>
    /// <response code="200">Logout successful — session revoked and JWT blacklisted</response>
    /// <response code="401">Unauthorized — invalid or expired token</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var deviceId = HttpContext.Items["deviceId"]?.ToString() ?? string.Empty;
        var jti = HttpContext.Items["jti"]?.ToString() ?? string.Empty;
        var tokenExpiry = HttpContext.Items.TryGetValue("tokenExpiry", out var exp) && exp is DateTime dt
            ? dt
            : DateTime.UtcNow.AddMinutes(15);

        await _authService.LogoutAsync(userId, deviceId, jti, tokenExpiry, ct);

        var apiResponse = ApiResponse<object>.Ok(null!, "Logout successful.");
        apiResponse.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(apiResponse);
    }

    /// <summary>
    /// Refresh the access token using a valid refresh token.
    /// </summary>
    /// <param name="request">Refresh token and device ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>New JWT access token and rotated refresh token</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="401">Refresh token is invalid, expired, or reused</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/auth/refresh
    ///     {
    ///         "refreshToken": "eyJhbGciOi...",
    ///         "deviceId": "device-001"
    ///     }
    ///
    /// Uses refresh token rotation — the old refresh token is invalidated.
    /// Reuse of an already-rotated token triggers revocation of all user sessions.
    /// </remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, request.DeviceId, ct);

        var response = new LoginResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn,
            IsFirstTimeUser = result.IsFirstTimeUser
        };

        var apiResponse = ApiResponse<LoginResponse>.Ok(response, "Token refreshed.");
        apiResponse.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(apiResponse);
    }

    /// <summary>
    /// Request a one-time password (OTP) for the given identity.
    /// </summary>
    /// <param name="request">Identity (email) to send OTP to</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation that OTP was sent</returns>
    /// <response code="200">OTP sent successfully</response>
    /// <response code="429">Too many OTP requests</response>
    [HttpPost("otp/request")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<object>>> RequestOtp(
        [FromBody] OtpRequest request, CancellationToken ct)
    {
        await _otpService.GenerateOtpAsync(request.Identity, ct);

        var apiResponse = ApiResponse<object>.Ok(null!, "OTP sent successfully.");
        apiResponse.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(apiResponse);
    }

    /// <summary>
    /// Verify a one-time password (OTP) code.
    /// </summary>
    /// <param name="request">Identity and 6-digit OTP code</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of OTP verification</returns>
    /// <response code="200">OTP verified successfully</response>
    /// <response code="400">OTP expired or verification failed</response>
    /// <response code="429">Maximum OTP verification attempts exceeded</response>
    [HttpPost("otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<object>>> VerifyOtp(
        [FromBody] OtpVerifyRequest request, CancellationToken ct)
    {
        await _otpService.VerifyOtpAsync(request.Identity, request.Code, ct);

        var apiResponse = ApiResponse<object>.Ok(null!, "OTP verified successfully.");
        apiResponse.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(apiResponse);
    }

    /// <summary>
    /// Generate credentials for a new team member (service-to-service).
    /// </summary>
    /// <param name="request">Member ID and email for credential generation</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of credential generation</returns>
    /// <response code="200">Credentials generated successfully</response>
    /// <response code="403">Service not authorized</response>
    [HttpPost("credentials/generate")]
    [ServiceAuth]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> GenerateCredentials(
        [FromBody] CredentialGenerateRequest request, CancellationToken ct)
    {
        await _authService.GenerateCredentialsAsync(request.MemberId, request.Email, ct);

        var apiResponse = ApiResponse<object>.Ok(null!, "Credentials generated successfully.");
        apiResponse.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(apiResponse);
    }
}
