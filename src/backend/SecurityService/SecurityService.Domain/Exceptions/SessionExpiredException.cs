using System.Net;

namespace SecurityService.Domain.Exceptions;

public class SessionExpiredException : DomainException
{
    public SessionExpiredException(string message = "Session has expired.")
        : base(ErrorCodes.SessionExpiredValue, ErrorCodes.SessionExpired, message, HttpStatusCode.Unauthorized)
    {
    }
}
