using FluentValidation;
using WorkService.Application.DTOs.SavedFilters;

namespace WorkService.Application.Validators;

public class CreateSavedFilterRequestValidator : AbstractValidator<CreateSavedFilterRequest>
{
    public CreateSavedFilterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Filters).NotEmpty();
    }
}
