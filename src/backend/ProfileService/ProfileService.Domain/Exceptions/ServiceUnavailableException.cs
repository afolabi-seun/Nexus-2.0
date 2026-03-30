using System.Net;

namespace ProfileService.Domain.Exceptions;

public class ServiceUnavailableException : DomainException
{
    public ServiceUnavailableException(string message = "The downstream service is currently unavailable.")
        : base(ErrorCodes.ServiceUnavailableValue, ErrorCodes.ServiceUnavailable, message, HttpStatusCode.ServiceUnavailable)
    {
    }
}
