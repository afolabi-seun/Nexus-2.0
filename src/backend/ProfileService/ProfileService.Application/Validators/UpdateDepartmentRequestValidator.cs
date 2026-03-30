using FluentValidation;
using ProfileService.Application.DTOs.Departments;

namespace ProfileService.Application.Validators;

public class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x.DepartmentName).MaximumLength(100).When(x => x.DepartmentName != null);
    }
}
