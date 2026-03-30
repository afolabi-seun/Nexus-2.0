using System.Net;

namespace SecurityService.Domain.Exceptions;

public class PasswordReuseNotAllowedException : DomainException
{
    public PasswordReuseNotAllowedException(string message = "New password cannot be the same as the current password.")
        : base(ErrorCodes.PasswordReuseNotAllowedValue, ErrorCodes.PasswordReuseNotAllowed, message, HttpStatusCode.BadRequest)
    {
    }
}
