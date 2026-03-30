using FluentValidation.TestHelper;
using UtilityService.Application.DTOs.AuditLogs;
using UtilityService.Application.DTOs.ErrorCodes;
using UtilityService.Application.DTOs.ErrorLogs;
using UtilityService.Application.DTOs.Notifications;
using UtilityService.Application.DTOs.ReferenceData;
using UtilityService.Application.Validators;

namespace UtilityService.Tests.Validators;

public class FluentValidationTests
{
    // --- CreateAuditLogRequestValidator ---
    private readonly CreateAuditLogRequestValidator _auditLogValidator = new();

    [Fact]
    public void AuditLogValidator_ValidRequest_Passes()
    {
        var request = new CreateAuditLogRequest
        {
            OrganizationId = Guid.NewGuid(), ServiceName = "Svc", Action = "Create",
            EntityType = "User", EntityId = "123", UserId = "u1", CorrelationId = "corr-1"
        };
        var result = _auditLogValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AuditLogValidator_EmptyFields_Fails()
    {
        var result = _auditLogValidator.TestValidate(new CreateAuditLogRequest());
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
        result.ShouldHaveValidationErrorFor(x => x.ServiceName);
        result.ShouldHaveValidationErrorFor(x => x.Action);
        result.ShouldHaveValidationErrorFor(x => x.EntityType);
        result.ShouldHaveValidationErrorFor(x => x.EntityId);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
        result.ShouldHaveValidationErrorFor(x => x.CorrelationId);
    }

    // --- CreateErrorLogRequestValidator ---
    private readonly CreateErrorLogRequestValidator _errorLogValidator = new();

    [Fact]
    public void ErrorLogValidator_ValidRequest_Passes()
    {
        var request = new CreateErrorLogRequest
        {
            OrganizationId = Guid.NewGuid(), ServiceName = "Svc", ErrorCode = "ERR",
            Message = "msg", CorrelationId = "c1", Severity = "Error"
        };
        _errorLogValidator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Info")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Critical")]
    public void ErrorLogValidator_ValidSeverity_Passes(string severity)
    {
        var request = new CreateErrorLogRequest
        {
            OrganizationId = Guid.NewGuid(), ServiceName = "Svc", ErrorCode = "ERR",
            Message = "msg", CorrelationId = "c1", Severity = severity
        };
        _errorLogValidator.TestValidate(request).ShouldNotHaveValidationErrorFor(x => x.Severity);
    }

    [Theory]
    [InlineData("info")]
    [InlineData("DEBUG")]
    [InlineData("Fatal")]
    [InlineData("")]
    public void ErrorLogValidator_InvalidSeverity_Fails(string severity)
    {
        var request = new CreateErrorLogRequest
        {
            OrganizationId = Guid.NewGuid(), ServiceName = "Svc", ErrorCode = "ERR",
            Message = "msg", CorrelationId = "c1", Severity = severity
        };
        _errorLogValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Severity);
    }

    // --- CreateErrorCodeRequestValidator ---
    private readonly CreateErrorCodeRequestValidator _errorCodeValidator = new();

    [Fact]
    public void ErrorCodeValidator_ValidRequest_Passes()
    {
        var request = new CreateErrorCodeRequest
        {
            Code = "ERR_001", Value = 1, HttpStatusCode = 400,
            ResponseCode = "01", Description = "Desc", ServiceName = "Svc"
        };
        _errorCodeValidator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ErrorCodeValidator_ValueZero_Fails()
    {
        var request = new CreateErrorCodeRequest
        {
            Code = "ERR", Value = 0, HttpStatusCode = 400,
            ResponseCode = "01", Description = "Desc", ServiceName = "Svc"
        };
        _errorCodeValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Value);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public void ErrorCodeValidator_HttpStatusCodeOutOfRange_Fails(int statusCode)
    {
        var request = new CreateErrorCodeRequest
        {
            Code = "ERR", Value = 1, HttpStatusCode = statusCode,
            ResponseCode = "01", Description = "Desc", ServiceName = "Svc"
        };
        _errorCodeValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.HttpStatusCode);
    }

    [Fact]
    public void ErrorCodeValidator_ResponseCodeTooLong_Fails()
    {
        var request = new CreateErrorCodeRequest
        {
            Code = "ERR", Value = 1, HttpStatusCode = 400,
            ResponseCode = "12345678901", Description = "Desc", ServiceName = "Svc"
        };
        _errorCodeValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.ResponseCode);
    }

    // --- UpdateErrorCodeRequestValidator ---
    private readonly UpdateErrorCodeRequestValidator _updateErrorCodeValidator = new();

    [Fact]
    public void UpdateErrorCodeValidator_AllNull_Passes()
    {
        _updateErrorCodeValidator.TestValidate(new UpdateErrorCodeRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateErrorCodeValidator_HttpStatusCodeOutOfRange_Fails()
    {
        var request = new UpdateErrorCodeRequest { HttpStatusCode = 700 };
        _updateErrorCodeValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.HttpStatusCode);
    }

    [Fact]
    public void UpdateErrorCodeValidator_ResponseCodeTooLong_Fails()
    {
        var request = new UpdateErrorCodeRequest { ResponseCode = "12345678901" };
        _updateErrorCodeValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.ResponseCode);
    }

    // --- DispatchNotificationRequestValidator ---
    private readonly DispatchNotificationRequestValidator _notifValidator = new();

    [Fact]
    public void NotificationValidator_ValidRequest_Passes()
    {
        var request = new DispatchNotificationRequest
        {
            OrganizationId = Guid.NewGuid(), UserId = Guid.NewGuid(),
            NotificationType = "StoryAssigned", Channels = "Email,Push",
            Recipient = "user@test.com"
        };
        _notifValidator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("StoryAssigned")]
    [InlineData("TaskAssigned")]
    [InlineData("SprintStarted")]
    [InlineData("SprintEnded")]
    [InlineData("MentionedInComment")]
    [InlineData("StoryStatusChanged")]
    [InlineData("TaskStatusChanged")]
    [InlineData("DueDateApproaching")]
    public void NotificationValidator_ValidTypes_Pass(string type)
    {
        var request = new DispatchNotificationRequest
        {
            OrganizationId = Guid.NewGuid(), UserId = Guid.NewGuid(),
            NotificationType = type, Channels = "Email", Recipient = "r@t.com"
        };
        _notifValidator.TestValidate(request).ShouldNotHaveValidationErrorFor(x => x.NotificationType);
    }

    [Theory]
    [InlineData("InvalidType")]
    [InlineData("")]
    public void NotificationValidator_InvalidType_Fails(string type)
    {
        var request = new DispatchNotificationRequest
        {
            OrganizationId = Guid.NewGuid(), UserId = Guid.NewGuid(),
            NotificationType = type, Channels = "Email", Recipient = "r@t.com"
        };
        _notifValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.NotificationType);
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("Push")]
    [InlineData("InApp")]
    [InlineData("Email,Push,InApp")]
    public void NotificationValidator_ValidChannels_Pass(string channels)
    {
        var request = new DispatchNotificationRequest
        {
            OrganizationId = Guid.NewGuid(), UserId = Guid.NewGuid(),
            NotificationType = "StoryAssigned", Channels = channels, Recipient = "r@t.com"
        };
        _notifValidator.TestValidate(request).ShouldNotHaveValidationErrorFor(x => x.Channels);
    }

    [Theory]
    [InlineData("SMS")]
    [InlineData("Email,SMS")]
    public void NotificationValidator_InvalidChannels_Fail(string channels)
    {
        var request = new DispatchNotificationRequest
        {
            OrganizationId = Guid.NewGuid(), UserId = Guid.NewGuid(),
            NotificationType = "StoryAssigned", Channels = channels, Recipient = "r@t.com"
        };
        _notifValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Channels);
    }

    // --- CreateDepartmentTypeRequestValidator ---
    private readonly CreateDepartmentTypeRequestValidator _deptValidator = new();

    [Fact]
    public void DeptTypeValidator_ValidRequest_Passes()
    {
        var request = new CreateDepartmentTypeRequest { TypeName = "Engineering", TypeCode = "ENG" };
        _deptValidator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("eng")]
    [InlineData("Eng-1")]
    [InlineData("ENG 1")]
    public void DeptTypeValidator_InvalidTypeCode_Fails(string code)
    {
        var request = new CreateDepartmentTypeRequest { TypeName = "Test", TypeCode = code };
        _deptValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.TypeCode);
    }

    [Fact]
    public void DeptTypeValidator_TypeCodeTooLong_Fails()
    {
        var request = new CreateDepartmentTypeRequest { TypeName = "Test", TypeCode = "ABCDEFGHIJK" };
        _deptValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.TypeCode);
    }

    // --- CreatePriorityLevelRequestValidator ---
    private readonly CreatePriorityLevelRequestValidator _priorityValidator = new();

    [Fact]
    public void PriorityValidator_ValidRequest_Passes()
    {
        var request = new CreatePriorityLevelRequest { Name = "Critical", SortOrder = 1, Color = "#DC2626" };
        _priorityValidator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PriorityValidator_SortOrderZero_Fails()
    {
        var request = new CreatePriorityLevelRequest { Name = "Test", SortOrder = 0, Color = "#FF0000" };
        _priorityValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.SortOrder);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#GGG000")]
    [InlineData("#FFF")]
    [InlineData("DC2626")]
    public void PriorityValidator_InvalidHexColor_Fails(string color)
    {
        var request = new CreatePriorityLevelRequest { Name = "Test", SortOrder = 1, Color = color };
        _priorityValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Color);
    }

    [Theory]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#abcdef")]
    [InlineData("#DC2626")]
    public void PriorityValidator_ValidHexColor_Passes(string color)
    {
        var request = new CreatePriorityLevelRequest { Name = "Test", SortOrder = 1, Color = color };
        _priorityValidator.TestValidate(request).ShouldNotHaveValidationErrorFor(x => x.Color);
    }
}
