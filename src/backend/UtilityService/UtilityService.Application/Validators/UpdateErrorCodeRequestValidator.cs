using FluentValidation;
using UtilityService.Application.DTOs.ErrorCodes;

namespace UtilityService.Application.Validators;

public class UpdateErrorCodeRequestValidator : AbstractValidator<UpdateErrorCodeRequest>
{
    public UpdateErrorCodeRequestValidator()
    {
        RuleFor(x => x.HttpStatusCode).InclusiveBetween(100, 599).When(x => x.HttpStatusCode.HasValue);
        RuleFor(x => x.ResponseCode).MaximumLength(10).When(x => x.ResponseCode != null);
    }
}
