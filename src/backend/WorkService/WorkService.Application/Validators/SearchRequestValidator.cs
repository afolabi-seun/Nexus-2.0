using FluentValidation;
using WorkService.Application.DTOs.Search;

namespace WorkService.Application.Validators;

public class SearchRequestValidator : AbstractValidator<SearchRequest>
{
    public SearchRequestValidator()
    {
        RuleFor(x => x.Query).MinimumLength(2).When(x => x.Query != null);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
    }
}
