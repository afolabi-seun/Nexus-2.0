using System.Net;

namespace BillingService.Domain.Exceptions;

public class SubscriptionNotFoundException : DomainException
{
    public SubscriptionNotFoundException()
        : base(ErrorCodes.SubscriptionNotFoundValue, ErrorCodes.SubscriptionNotFound,
            "No subscription found for the organization.", HttpStatusCode.NotFound) { }
}
