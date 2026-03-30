namespace BillingService.Domain.Exceptions;

public class InvalidWebhookPayloadException : DomainException
{
    public InvalidWebhookPayloadException()
        : base(ErrorCodes.InvalidWebhookPayloadValue, ErrorCodes.InvalidWebhookPayload,
            "Webhook payload could not be deserialized.") { }
}
