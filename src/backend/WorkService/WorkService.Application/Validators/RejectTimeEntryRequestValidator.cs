using FluentValidation;
using WorkService.Application.DTOs.TimeEntries;

namespace WorkService.Application.Validators;

public class RejectTimeEntryRequestValidator : AbstractValidator<RejectTimeEntryRequest>
{
    public RejectTimeEntryRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}
