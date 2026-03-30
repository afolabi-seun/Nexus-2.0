using System.Net;

namespace BillingService.Domain.Exceptions;

public class SubscriptionAlreadyExistsException : DomainException
{
    public SubscriptionAlreadyExistsException()
        : base(ErrorCodes.SubscriptionAlreadyExistsValue, ErrorCodes.SubscriptionAlreadyExists,
            "Organization already has an active subscription.", HttpStatusCode.Conflict) { }
}
