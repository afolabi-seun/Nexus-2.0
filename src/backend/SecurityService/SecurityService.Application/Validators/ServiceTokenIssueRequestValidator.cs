using FluentValidation;
using SecurityService.Application.DTOs.ServiceToken;

namespace SecurityService.Application.Validators;

public class ServiceTokenIssueRequestValidator : AbstractValidator<ServiceTokenIssueRequest>
{
    public ServiceTokenIssueRequestValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.ServiceName).NotEmpty();
    }
}
