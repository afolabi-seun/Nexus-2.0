using FluentValidation;
using WorkService.Application.DTOs.TimeEntries;

namespace WorkService.Application.Validators;

public class RejectTimeEntryRequestValidator : AbstractValidator<RejectTimeEntryRequest>
{
    public RejectTimeEntryRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty();
    }
}
