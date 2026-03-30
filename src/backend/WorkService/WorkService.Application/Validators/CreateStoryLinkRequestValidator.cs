using FluentValidation;
using WorkService.Application.DTOs.Stories;

namespace WorkService.Application.Validators;

public class CreateStoryLinkRequestValidator : AbstractValidator<CreateStoryLinkRequest>
{
    private static readonly HashSet<string> ValidLinkTypes = new() { "blocks", "is_blocked_by", "relates_to", "duplicates" };

    public CreateStoryLinkRequestValidator()
    {
        RuleFor(x => x.TargetStoryId).NotEmpty();
        RuleFor(x => x.LinkType).NotEmpty().Must(v => ValidLinkTypes.Contains(v))
            .WithMessage("LinkType must be one of: blocks, is_blocked_by, relates_to, duplicates.");
    }
}
