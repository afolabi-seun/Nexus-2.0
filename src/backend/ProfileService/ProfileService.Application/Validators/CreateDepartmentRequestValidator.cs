using FluentValidation;
using ProfileService.Application.DTOs.Departments;

namespace ProfileService.Application.Validators;

public class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentRequestValidator()
    {
        RuleFor(x => x.DepartmentName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentCode).NotEmpty().MaximumLength(20).Matches(@"^[A-Z0-9_]+$")
            .WithMessage("DepartmentCode must be uppercase alphanumeric with underscores.");
    }
}
