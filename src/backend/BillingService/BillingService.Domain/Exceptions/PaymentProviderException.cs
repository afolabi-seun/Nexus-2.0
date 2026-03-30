using System.Net;

namespace BillingService.Domain.Exceptions;

public class PaymentProviderException : DomainException
{
    public PaymentProviderException(string message)
        : base(ErrorCodes.PaymentProviderErrorValue, ErrorCodes.PaymentProviderError,
            message, HttpStatusCode.BadGateway) { }
}
