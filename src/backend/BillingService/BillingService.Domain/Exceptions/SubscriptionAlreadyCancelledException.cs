namespace BillingService.Domain.Exceptions;

public class SubscriptionAlreadyCancelledException : DomainException
{
    public SubscriptionAlreadyCancelledException()
        : base(ErrorCodes.SubscriptionAlreadyCancelledValue, ErrorCodes.SubscriptionAlreadyCancelled,
            "Subscription is already cancelled.") { }
}
