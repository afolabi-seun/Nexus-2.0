using FluentValidation;
using WorkService.Application.DTOs.Sprints;

namespace WorkService.Application.Validators;

public class AddStoryToSprintRequestValidator : AbstractValidator<AddStoryToSprintRequest>
{
    public AddStoryToSprintRequestValidator()
    {
        RuleFor(x => x.StoryId).NotEmpty();
    }
}
