using FluentValidation;
using WorkService.Application.DTOs.TimePolicies;

namespace WorkService.Application.Validators;

public class UpdateTimePolicyRequestValidator : AbstractValidator<UpdateTimePolicyRequest>
{
    private static readonly string[] AllowedWorkflows = ["None", "DeptLeadApproval", "ProjectLeadApproval"];

    public UpdateTimePolicyRequestValidator()
    {
        RuleFor(x => x.RequiredHoursPerDay).GreaterThan(0).LessThanOrEqualTo(24)
            .WithMessage("RequiredHoursPerDay must be between 0 (exclusive) and 24 (inclusive).");
        RuleFor(x => x.MaxDailyHours).GreaterThanOrEqualTo(x => x.RequiredHoursPerDay)
            .WithMessage("MaxDailyHours must be greater than or equal to RequiredHoursPerDay.");
        RuleFor(x => x.ApprovalWorkflow).Must(w => AllowedWorkflows.Contains(w))
            .WithMessage("ApprovalWorkflow must be one of: None, DeptLeadApproval, ProjectLeadApproval.");
    }
}
