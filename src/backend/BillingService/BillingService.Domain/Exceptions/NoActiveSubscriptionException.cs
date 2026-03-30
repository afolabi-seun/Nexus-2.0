namespace BillingService.Domain.Exceptions;

public class NoActiveSubscriptionException : DomainException
{
    public NoActiveSubscriptionException()
        : base(ErrorCodes.NoActiveSubscriptionValue, ErrorCodes.NoActiveSubscription,
            "Organization has no active or trialing subscription.") { }
}
