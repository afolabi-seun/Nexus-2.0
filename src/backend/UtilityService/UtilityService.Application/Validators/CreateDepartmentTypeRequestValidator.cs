using FluentValidation;
using UtilityService.Application.DTOs.ReferenceData;

namespace UtilityService.Application.Validators;

public class CreateDepartmentTypeRequestValidator : AbstractValidator<CreateDepartmentTypeRequest>
{
    public CreateDepartmentTypeRequestValidator()
    {
        RuleFor(x => x.TypeName).NotEmpty();
        RuleFor(x => x.TypeCode).NotEmpty().MaximumLength(10).Matches(@"^[A-Z0-9]+$")
            .WithMessage("TypeCode must be uppercase alphanumeric.");
    }
}
