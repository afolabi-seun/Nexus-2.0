using FluentValidation;
using WorkService.Application.DTOs.RiskRegisters;

namespace WorkService.Application.Validators;

public class CreateRiskRequestValidator : AbstractValidator<CreateRiskRequest>
{
    private static readonly string[] AllowedSeverities = ["Low", "Medium", "High", "Critical"];
    private static readonly string[] AllowedLikelihoods = ["Low", "Medium", "High"];
    private static readonly string[] AllowedMitigationStatuses = ["Open", "Mitigating", "Mitigated", "Accepted"];

    public CreateRiskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.Severity)
            .Must(s => AllowedSeverities.Contains(s))
            .WithMessage("Severity must be one of: Low, Medium, High, Critical.");

        RuleFor(x => x.Likelihood)
            .Must(l => AllowedLikelihoods.Contains(l))
            .WithMessage("Likelihood must be one of: Low, Medium, High.");

        RuleFor(x => x.MitigationStatus)
            .Must(m => AllowedMitigationStatuses.Contains(m))
            .WithMessage("MitigationStatus must be one of: Open, Mitigating, Mitigated, Accepted.");
    }
}
