using System.Net;

namespace SecurityService.Domain.Exceptions;

public class ServiceNotAuthorizedException : DomainException
{
    public ServiceNotAuthorizedException(string message = "Service is not authorized to perform this action.")
        : base(ErrorCodes.ServiceNotAuthorizedValue, ErrorCodes.ServiceNotAuthorized, message, HttpStatusCode.Forbidden)
    {
    }
}
