using FluentValidation;
using WorkService.Application.DTOs.Comments;

namespace WorkService.Application.Validators;

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.EntityType).NotEmpty().Must(v => v is "Story" or "Task");
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000).WithMessage("Comment must not exceed 5000 characters.");
    }
}
