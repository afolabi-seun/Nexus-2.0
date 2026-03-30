using FluentValidation;
using WorkService.Application.DTOs.Stories;

namespace WorkService.Application.Validators;

public class UpdateStoryRequestValidator : AbstractValidator<UpdateStoryRequest>
{
    private static readonly HashSet<int> FibonacciPoints = new() { 1, 2, 3, 5, 8, 13, 21 };
    private static readonly HashSet<string> ValidPriorities = new() { "Critical", "High", "Medium", "Low" };

    public UpdateStoryRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title != null);
        RuleFor(x => x.Description).MaximumLength(5000).When(x => x.Description != null);
        RuleFor(x => x.StoryPoints).Must(v => FibonacciPoints.Contains(v!.Value))
            .When(x => x.StoryPoints.HasValue);
        RuleFor(x => x.Priority).Must(v => ValidPriorities.Contains(v!))
            .When(x => x.Priority != null);
    }
}
