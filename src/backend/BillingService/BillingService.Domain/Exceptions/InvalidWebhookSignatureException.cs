namespace BillingService.Domain.Exceptions;

public class InvalidWebhookSignatureException : DomainException
{
    public InvalidWebhookSignatureException()
        : base(ErrorCodes.InvalidWebhookSignatureValue, ErrorCodes.InvalidWebhookSignature,
            "Stripe webhook signature verification failed.") { }
}
