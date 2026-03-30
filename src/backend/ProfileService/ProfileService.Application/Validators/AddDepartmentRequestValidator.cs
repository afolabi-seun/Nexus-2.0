using FluentValidation;
using ProfileService.Application.DTOs.TeamMembers;

namespace ProfileService.Application.Validators;

public class AddDepartmentRequestValidator : AbstractValidator<AddDepartmentRequest>
{
    public AddDepartmentRequestValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
