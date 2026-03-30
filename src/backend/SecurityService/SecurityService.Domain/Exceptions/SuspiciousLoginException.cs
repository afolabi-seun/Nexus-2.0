using System.Net;

namespace SecurityService.Domain.Exceptions;

public class SuspiciousLoginException : DomainException
{
    public SuspiciousLoginException(string message = "Suspicious login activity detected.")
        : base(ErrorCodes.SuspiciousLoginValue, ErrorCodes.SuspiciousLogin, message, HttpStatusCode.Forbidden)
    {
    }
}
