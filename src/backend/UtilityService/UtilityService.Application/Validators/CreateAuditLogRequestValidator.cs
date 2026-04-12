using FluentValidation;
using UtilityService.Application.DTOs.AuditLogs;

namespace UtilityService.Application.Validators;

public class CreateAuditLogRequestValidator : AbstractValidator<CreateAuditLogRequest>
{
    public CreateAuditLogRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Action).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EntityType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EntityId).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CorrelationId).NotEmpty().MaximumLength(100);
    }
}
