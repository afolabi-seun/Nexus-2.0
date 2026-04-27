using FsCheck;
using FsCheck.Xunit;
using FluentValidation;
using Moq;
using SecurityService.Application.DTOs.Password;
using SecurityService.Application.Validators;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Repositories.PasswordHistory;
using SecurityService.Domain.Interfaces.Services.Otp;
using SecurityService.Domain.Interfaces.Services.Outbox;
using SecurityService.Domain.Interfaces.Services.Password;
using SecurityService.Infrastructure.Configuration;
using SecurityService.Infrastructure.Data;
using SecurityService.Infrastructure.Services.Password;

using SecurityService.Infrastructure.Services.ServiceClients;

namespace SecurityService.Tests.Validators;

/// <summary>
/// Property-based tests for password complexity validation rules.
/// Property: Any string meeting all complexity rules (≥8 chars, uppercase, lowercase, digit, special)
/// passes validation; any string missing at least one rule fails.
/// Validates: REQ-011.1, REQ-011.2
/// </summary>
public class PasswordComplexityPropertyTests
{
    private static readonly ForcedPasswordChangeRequestValidator _validator = new();
    private static readonly PasswordService _passwordService = CreatePasswordService();

    private static readonly char[] UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] LowerChars = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
    private static readonly char[] DigitChars = "0123456789".ToCharArray();
    private static readonly char[] SpecialChars = "!@#$%^&*".ToCharArray();
    private static readonly char[] AllChars = UpperChars.Concat(LowerChars).Concat(DigitChars).Concat(SpecialChars).ToArray();

    private static PasswordService CreatePasswordService()
    {
        var repo = new Mock<IPasswordHistoryRepository>();
        var dbContext = new Mock<SecurityDbContext>(
            new Microsoft.EntityFrameworkCore.DbContextOptions<SecurityDbContext>(),
            (string?)null);
        var otp = new Mock<IOtpService>();
        var outbox = new Mock<IOutboxService>();
        var profileClient = new Mock<IProfileServiceClient>();
        var settings = new AppSettings();
        return new PasswordService(repo.Object, dbContext.Object, otp.Object, outbox.Object, profileClient.Object, settings);
    }

    // ── FluentValidation property tests ──

    /// <summary>
    /// Property: Any string with ≥8 chars containing at least one uppercase, lowercase, digit, and special char
    /// passes FluentValidation.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool ValidPassword_AlwaysPassesFluentValidation(ushort seed)
    {
        var rng = new Random(seed);
        var password = GenerateValidPassword(rng);
        var request = new ForcedPasswordChangeRequest { NewPassword = password };
        var result = _validator.Validate(request);
        return result.IsValid;
    }

    /// <summary>
    /// Property: Any string missing at least one complexity rule fails FluentValidation.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool InvalidPassword_MissingRule_FailsFluentValidation(ushort seed)
    {
        var rng = new Random(seed);
        var missingRule = rng.Next(0, 4);
        var password = GeneratePasswordMissingRule(rng, missingRule);
        var request = new ForcedPasswordChangeRequest { NewPassword = password };
        var result = _validator.Validate(request);
        return !result.IsValid;
    }

    // ── PasswordService.ValidateComplexity property tests ──

    /// <summary>
    /// Property: Any string meeting all complexity rules passes PasswordService.ValidateComplexity
    /// without throwing PasswordComplexityFailedException.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool ValidPassword_AlwaysPassesServiceValidation(ushort seed)
    {
        var rng = new Random(seed);
        var password = GenerateValidPassword(rng);
        try
        {
            return _passwordService.ValidateComplexity(password);
        }
        catch (PasswordComplexityFailedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Property: Any string missing at least one complexity rule causes PasswordService.ValidateComplexity
    /// to throw PasswordComplexityFailedException.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool InvalidPassword_MissingRule_FailsServiceValidation(ushort seed)
    {
        var rng = new Random(seed);
        var missingRule = rng.Next(0, 4);
        var password = GeneratePasswordMissingRule(rng, missingRule);
        try
        {
            _passwordService.ValidateComplexity(password);
            return false;
        }
        catch (PasswordComplexityFailedException)
        {
            return true;
        }
    }

    /// <summary>
    /// Property: Any password shorter than 8 characters always fails validation,
    /// regardless of character composition.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool ShortPassword_AlwaysFailsValidation(ushort seed)
    {
        var rng = new Random(seed);
        var length = rng.Next(1, 8); // 1–7 chars
        var chars = new char[length];

        // Even if it has all character types, it should still fail
        if (length >= 4)
        {
            chars[0] = UpperChars[rng.Next(UpperChars.Length)];
            chars[1] = LowerChars[rng.Next(LowerChars.Length)];
            chars[2] = DigitChars[rng.Next(DigitChars.Length)];
            chars[3] = SpecialChars[rng.Next(SpecialChars.Length)];
            for (int i = 4; i < length; i++)
                chars[i] = AllChars[rng.Next(AllChars.Length)];
        }
        else
        {
            for (int i = 0; i < length; i++)
                chars[i] = AllChars[rng.Next(AllChars.Length)];
        }

        var password = new string(chars);
        var request = new ForcedPasswordChangeRequest { NewPassword = password };
        var fluentFails = !_validator.Validate(request).IsValid;

        bool serviceFails;
        try
        {
            _passwordService.ValidateComplexity(password);
            serviceFails = false;
        }
        catch (PasswordComplexityFailedException)
        {
            serviceFails = true;
        }

        return fluentFails && serviceFails;
    }

    /// <summary>
    /// Property: FluentValidation and PasswordService.ValidateComplexity agree on all valid passwords.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool FluentAndService_AgreeOnValidPasswords(ushort seed)
    {
        var rng = new Random(seed);
        var password = GenerateValidPassword(rng);
        var request = new ForcedPasswordChangeRequest { NewPassword = password };
        var fluentPasses = _validator.Validate(request).IsValid;

        bool servicePasses;
        try
        {
            servicePasses = _passwordService.ValidateComplexity(password);
        }
        catch (PasswordComplexityFailedException)
        {
            servicePasses = false;
        }

        return fluentPasses && servicePasses;
    }

    // ── Generators ──

    private static string GenerateValidPassword(Random rng)
    {
        var length = rng.Next(8, 20);
        var chars = new char[length];

        // Ensure at least one of each required type
        chars[0] = UpperChars[rng.Next(UpperChars.Length)];
        chars[1] = LowerChars[rng.Next(LowerChars.Length)];
        chars[2] = DigitChars[rng.Next(DigitChars.Length)];
        chars[3] = SpecialChars[rng.Next(SpecialChars.Length)];

        for (int i = 4; i < length; i++)
            chars[i] = AllChars[rng.Next(AllChars.Length)];

        // Shuffle to avoid predictable positions
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static string GeneratePasswordMissingRule(Random rng, int missingRule)
    {
        // 0 = no uppercase, 1 = no lowercase, 2 = no digit, 3 = no special
        var allowed = missingRule switch
        {
            0 => LowerChars.Concat(DigitChars).Concat(SpecialChars).ToArray(),
            1 => UpperChars.Concat(DigitChars).Concat(SpecialChars).ToArray(),
            2 => UpperChars.Concat(LowerChars).Concat(SpecialChars).ToArray(),
            3 => UpperChars.Concat(LowerChars).Concat(DigitChars).ToArray(),
            _ => throw new InvalidOperationException()
        };

        var length = rng.Next(8, 16);
        var chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = allowed[rng.Next(allowed.Length)];

        return new string(chars);
    }
}
