using System.Net;

namespace SecurityService.Domain.Exceptions;

public class ServiceUnavailableException : DomainException
{
    public ServiceUnavailableException(string message = "The service is currently unavailable.")
        : base(ErrorCodes.ServiceUnavailableValue, ErrorCodes.ServiceUnavailable, message, HttpStatusCode.ServiceUnavailable)
    {
    }
}
