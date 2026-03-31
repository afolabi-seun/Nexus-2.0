using System.Net;

namespace WorkService.Domain.Exceptions;

public class RiskNotFoundException : DomainException
{
    public RiskNotFoundException(Guid riskId)
        : base(ErrorCodes.RiskNotFoundValue, ErrorCodes.RiskNotFound,
            $"Risk register entry with ID '{riskId}' was not found.", HttpStatusCode.NotFound) { }
}
