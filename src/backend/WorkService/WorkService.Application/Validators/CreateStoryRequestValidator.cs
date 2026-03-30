using FluentValidation;
using WorkService.Application.DTOs.Stories;

namespace WorkService.Application.Validators;

public class CreateStoryRequestValidator : AbstractValidator<CreateStoryRequest>
{
    private static readonly HashSet<int> FibonacciPoints = new() { 1, 2, 3, 5, 8, 13, 21 };
    private static readonly HashSet<string> ValidPriorities = new() { "Critical", "High", "Medium", "Low" };

    public CreateStoryRequestValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(5000).When(x => x.Description != null);
        RuleFor(x => x.AcceptanceCriteria).MaximumLength(5000).When(x => x.AcceptanceCriteria != null);
        RuleFor(x => x.StoryPoints).Must(v => FibonacciPoints.Contains(v!.Value))
            .When(x => x.StoryPoints.HasValue)
            .WithMessage("Story points must be a Fibonacci number (1, 2, 3, 5, 8, 13, 21).");
        RuleFor(x => x.Priority).Must(v => ValidPriorities.Contains(v))
            .WithMessage("Priority must be one of: Critical, High, Medium, Low.");
    }
}
