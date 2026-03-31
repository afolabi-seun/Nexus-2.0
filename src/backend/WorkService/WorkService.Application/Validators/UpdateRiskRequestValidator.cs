using FluentValidation;
using WorkService.Application.DTOs.RiskRegisters;

namespace WorkService.Application.Validators;

public class UpdateRiskRequestValidator : AbstractValidator<UpdateRiskRequest>
{
    private static readonly string[] AllowedSeverities = ["Low", "Medium", "High", "Critical"];
    private static readonly string[] AllowedLikelihoods = ["Low", "Medium", "High"];
    private static readonly string[] AllowedMitigationStatuses = ["Open", "Mitigating", "Mitigated", "Accepted"];

    public UpdateRiskRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .When(x => x.Title != null);

        RuleFor(x => x.Severity)
            .Must(s => AllowedSeverities.Contains(s!))
            .WithMessage("Severity must be one of: Low, Medium, High, Critical.")
            .When(x => x.Severity != null);

        RuleFor(x => x.Likelihood)
            .Must(l => AllowedLikelihoods.Contains(l!))
            .WithMessage("Likelihood must be one of: Low, Medium, High.")
            .When(x => x.Likelihood != null);

        RuleFor(x => x.MitigationStatus)
            .Must(m => AllowedMitigationStatuses.Contains(m!))
            .WithMessage("MitigationStatus must be one of: Open, Mitigating, Mitigated, Accepted.")
            .When(x => x.MitigationStatus != null);
    }
}
