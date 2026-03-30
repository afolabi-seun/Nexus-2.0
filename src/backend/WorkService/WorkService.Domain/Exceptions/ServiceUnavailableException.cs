using System.Net;

namespace WorkService.Domain.Exceptions;

public class ServiceUnavailableException : DomainException
{
    public ServiceUnavailableException(string serviceName)
        : base(ErrorCodes.ServiceUnavailableValue, ErrorCodes.ServiceUnavailable,
            $"Service '{serviceName}' is currently unavailable.", HttpStatusCode.ServiceUnavailable) { }
}
