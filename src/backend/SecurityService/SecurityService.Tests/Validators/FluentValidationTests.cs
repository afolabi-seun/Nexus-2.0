using FluentValidation;
using SecurityService.Application.DTOs.Auth;
using SecurityService.Application.DTOs.Otp;
using SecurityService.Application.DTOs.Password;
using SecurityService.Application.Validators;

namespace SecurityService.Tests.Validators;

/// <summary>
/// Unit tests for FluentValidation validators.
/// Validates: REQ-023, REQ-011.1
/// </summary>
public class FluentValidationTests
{
    // --- LoginRequestValidator ---

    private readonly LoginRequestValidator _loginValidator = new();

    [Fact]
    public void LoginValidator_ValidInput_Passes()
    {
        var request = new LoginRequest { Email = "user@example.com", Password = "Password1!" };
        var result = _loginValidator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LoginValidator_EmptyEmail_Fails()
    {
        var request = new LoginRequest { Email = "", Password = "Password1!" };
        var result = _loginValidator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void LoginValidator_EmptyPassword_Fails()
    {
        var request = new LoginRequest { Email = "user@example.com", Password = "" };
        var result = _loginValidator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    // --- OtpVerifyRequestValidator ---

    private readonly OtpVerifyRequestValidator _otpValidator = new();

    [Fact]
    public void OtpValidator_6DigitCode_Passes()
    {
        var request = new OtpVerifyRequest { Identity = "user@example.com", Code = "123456" };
        var result = _otpValidator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void OtpValidator_5DigitCode_Fails()
    {
        var request = new OtpVerifyRequest { Identity = "user@example.com", Code = "12345" };
        var result = _otpValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void OtpValidator_NonNumericCode_Fails()
    {
        var request = new OtpVerifyRequest { Identity = "user@example.com", Code = "abcdef" };
        var result = _otpValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    // --- ForcedPasswordChangeRequestValidator ---

    private readonly ForcedPasswordChangeRequestValidator _passwordValidator = new();

    [Fact]
    public void PasswordValidator_ComplexPassword_Passes()
    {
        var request = new ForcedPasswordChangeRequest { NewPassword = "StrongP@ss1" };
        var result = _passwordValidator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void PasswordValidator_ShortPassword_Fails()
    {
        var request = new ForcedPasswordChangeRequest { NewPassword = "Sh@1" };
        var result = _passwordValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void PasswordValidator_NoUppercase_Fails()
    {
        var request = new ForcedPasswordChangeRequest { NewPassword = "lowercase1!" };
        var result = _passwordValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void PasswordValidator_NoSpecialChar_Fails()
    {
        var request = new ForcedPasswordChangeRequest { NewPassword = "NoSpecial1A" };
        var result = _passwordValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    // --- PasswordResetConfirmRequestValidator ---

    private readonly PasswordResetConfirmRequestValidator _resetValidator = new();

    [Fact]
    public void ResetValidator_ValidInputs_Passes()
    {
        var request = new PasswordResetConfirmRequest
        {
            Email = "user@example.com",
            OtpCode = "123456",
            NewPassword = "NewP@ssw0rd"
        };
        var result = _resetValidator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ResetValidator_InvalidOtpFormat_Fails()
    {
        var request = new PasswordResetConfirmRequest
        {
            Email = "user@example.com",
            OtpCode = "abc",
            NewPassword = "NewP@ssw0rd"
        };
        var result = _resetValidator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "OtpCode");
    }
}
