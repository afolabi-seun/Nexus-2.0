using FluentValidation;
using ProfileService.Application.DTOs.TeamMembers;

namespace ProfileService.Application.Validators;

public class ChangeRoleRequestValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleRequestValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
