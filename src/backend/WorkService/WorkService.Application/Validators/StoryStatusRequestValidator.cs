using FluentValidation;
using WorkService.Application.DTOs.Stories;

namespace WorkService.Application.Validators;

public class StoryStatusRequestValidator : AbstractValidator<StoryStatusRequest>
{
    public StoryStatusRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty();
    }
}
