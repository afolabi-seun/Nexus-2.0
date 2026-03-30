using FluentValidation;
using UtilityService.Application.DTOs.ErrorCodes;

namespace UtilityService.Application.Validators;

public class CreateErrorCodeRequestValidator : AbstractValidator<CreateErrorCodeRequest>
{
    public CreateErrorCodeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.HttpStatusCode).InclusiveBetween(100, 599);
        RuleFor(x => x.ResponseCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.ServiceName).NotEmpty();
    }
}
