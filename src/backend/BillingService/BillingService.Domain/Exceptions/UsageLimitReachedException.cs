using System.Net;

namespace BillingService.Domain.Exceptions;

public class UsageLimitReachedException : DomainException
{
    public UsageLimitReachedException(string feature)
        : base(ErrorCodes.UsageLimitReachedValue, ErrorCodes.UsageLimitReached,
            $"Usage limit reached for feature '{feature}'.", HttpStatusCode.Forbidden) { }
}
