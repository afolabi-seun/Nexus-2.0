using System.Net;

namespace BillingService.Domain.Exceptions;

public class FeatureNotAvailableException : DomainException
{
    public FeatureNotAvailableException(string feature)
        : base(ErrorCodes.FeatureNotAvailableValue, ErrorCodes.FeatureNotAvailable,
            $"Feature '{feature}' is not included in the current plan.", HttpStatusCode.Forbidden) { }
}
