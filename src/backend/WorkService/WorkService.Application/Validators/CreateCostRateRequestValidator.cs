using FluentValidation;
using WorkService.Application.DTOs.CostRates;

namespace WorkService.Application.Validators;

public class CreateCostRateRequestValidator : AbstractValidator<CreateCostRateRequest>
{
    private static readonly string[] AllowedRateTypes = ["Member", "RoleDepartment", "OrgDefault"];

    public CreateCostRateRequestValidator()
    {
        RuleFor(x => x.HourlyRate).GreaterThan(0).WithMessage("Hourly rate must be positive.");
        RuleFor(x => x.RateType).Must(rt => AllowedRateTypes.Contains(rt))
            .WithMessage("RateType must be one of: Member, RoleDepartment, OrgDefault.");

        RuleFor(x => x.MemberId).NotEmpty().WithMessage("MemberId is required for Member rate type.")
            .When(x => x.RateType == "Member");

        RuleFor(x => x.RoleName).NotEmpty().WithMessage("RoleName is required for RoleDepartment rate type.")
            .When(x => x.RateType == "RoleDepartment");
        RuleFor(x => x.DepartmentId).NotEmpty().WithMessage("DepartmentId is required for RoleDepartment rate type.")
            .When(x => x.RateType == "RoleDepartment");
    }
}
