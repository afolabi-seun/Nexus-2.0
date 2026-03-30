using FluentValidation;
using ProfileService.Application.DTOs.TeamMembers;

namespace ProfileService.Application.Validators;

public class UpdateTeamMemberRequestValidator : AbstractValidator<UpdateTeamMemberRequest>
{
    public UpdateTeamMemberRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName != null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName != null);
        RuleFor(x => x.MaxConcurrentTasks).GreaterThan(0).When(x => x.MaxConcurrentTasks.HasValue);
    }
}
