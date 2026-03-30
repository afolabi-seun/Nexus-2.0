using FluentValidation;
using UtilityService.Application.DTOs.ErrorLogs;

namespace UtilityService.Application.Validators;

public class CreateErrorLogRequestValidator : AbstractValidator<CreateErrorLogRequest>
{
    public CreateErrorLogRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.ServiceName).NotEmpty();
        RuleFor(x => x.ErrorCode).NotEmpty();
        RuleFor(x => x.Message).NotEmpty();
        RuleFor(x => x.CorrelationId).NotEmpty();
        RuleFor(x => x.Severity).NotEmpty()
            .Must(s => new[] { "Info", "Warning", "Error", "Critical" }.Contains(s))
            .WithMessage("Severity must be Info, Warning, Error, or Critical.");
    }
}
