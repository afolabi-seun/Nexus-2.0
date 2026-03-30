using FluentValidation.TestHelper;
using ProfileService.Application.DTOs.Invites;
using ProfileService.Application.DTOs.Organizations;
using ProfileService.Application.DTOs.Preferences;
using ProfileService.Application.DTOs.TeamMembers;
using ProfileService.Application.Validators;

namespace ProfileService.Tests.Validators;

public class CreateOrganizationRequestValidatorTests
{
    private readonly CreateOrganizationRequestValidator _validator = new();

    [Fact]
    public void ValidInput_ShouldPass()
    {
        var request = new CreateOrganizationRequest
        {
            OrganizationName = "Acme Corp",
            StoryIdPrefix = "ACME",
            TimeZone = "UTC",
            DefaultSprintDurationWeeks = 2
        };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_ShouldFail()
    {
        var request = new CreateOrganizationRequest
        {
            OrganizationName = "",
            StoryIdPrefix = "ACME",
            TimeZone = "UTC"
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.OrganizationName);
    }

    [Theory]
    [InlineData("acme")]       // lowercase
    [InlineData("A")]          // too short
    [InlineData("ABCDEFGHIJK")] // too long (11 chars)
    public void InvalidStoryIdPrefix_ShouldFail(string prefix)
    {
        var request = new CreateOrganizationRequest
        {
            OrganizationName = "Acme",
            StoryIdPrefix = prefix,
            TimeZone = "UTC"
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.StoryIdPrefix);
    }
}

public class AvailabilityRequestValidatorTests
{
    private readonly AvailabilityRequestValidator _validator = new();

    [Theory]
    [InlineData("Available")]
    [InlineData("Busy")]
    [InlineData("Away")]
    [InlineData("Offline")]
    public void ValidAvailability_ShouldPass(string value)
    {
        var request = new AvailabilityRequest { Availability = value };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Online")]
    [InlineData("invalid")]
    public void InvalidAvailability_ShouldFail(string value)
    {
        var request = new AvailabilityRequest { Availability = value };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Availability);
    }
}

public class UserPreferencesRequestValidatorTests
{
    private readonly UserPreferencesRequestValidator _validator = new();

    [Fact]
    public void ValidPreferences_ShouldPass()
    {
        var request = new UserPreferencesRequest
        {
            Theme = "Dark",
            DefaultBoardView = "Kanban",
            DateFormat = "ISO",
            TimeFormat = "H24"
        };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvalidTheme_ShouldFail()
    {
        var request = new UserPreferencesRequest { Theme = "Blue" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Theme);
    }

    [Fact]
    public void InvalidBoardView_ShouldFail()
    {
        var request = new UserPreferencesRequest { DefaultBoardView = "Table" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DefaultBoardView);
    }

    [Fact]
    public void InvalidDateFormat_ShouldFail()
    {
        var request = new UserPreferencesRequest { DateFormat = "YYYY" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DateFormat);
    }

    [Fact]
    public void InvalidTimeFormat_ShouldFail()
    {
        var request = new UserPreferencesRequest { TimeFormat = "AM" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TimeFormat);
    }
}

public class CreateInviteRequestValidatorTests
{
    private readonly CreateInviteRequestValidator _validator = new();

    [Fact]
    public void ValidInput_ShouldPass()
    {
        var request = new CreateInviteRequest
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            DepartmentId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyEmail_ShouldFail()
    {
        var request = new CreateInviteRequest
        {
            Email = "",
            FirstName = "John",
            LastName = "Doe",
            DepartmentId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}

public class AcceptInviteRequestValidatorTests
{
    private readonly AcceptInviteRequestValidator _validator = new();

    [Fact]
    public void SixDigitOtp_ShouldPass()
    {
        var request = new AcceptInviteRequest { OtpCode = "123456" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("abcdef")]
    [InlineData("12345")]
    [InlineData("1234567")]
    public void NonNumericOrWrongLength_ShouldFail(string otp)
    {
        var request = new AcceptInviteRequest { OtpCode = otp };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.OtpCode);
    }
}
