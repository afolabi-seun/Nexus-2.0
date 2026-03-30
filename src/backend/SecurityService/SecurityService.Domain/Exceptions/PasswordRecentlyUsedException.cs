using System.Net;

namespace SecurityService.Domain.Exceptions;

public class PasswordRecentlyUsedException : DomainException
{
    public PasswordRecentlyUsedException(string message = "Password was recently used. Please choose a different password.")
        : base(ErrorCodes.PasswordRecentlyUsedValue, ErrorCodes.PasswordRecentlyUsed, message, HttpStatusCode.BadRequest)
    {
    }
}
