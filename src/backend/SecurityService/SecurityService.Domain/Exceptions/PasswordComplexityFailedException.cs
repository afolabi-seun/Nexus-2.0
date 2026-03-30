using System.Net;

namespace SecurityService.Domain.Exceptions;

public class PasswordComplexityFailedException : DomainException
{
    public PasswordComplexityFailedException(string message = "Password does not meet complexity requirements.")
        : base(ErrorCodes.PasswordComplexityFailedValue, ErrorCodes.PasswordComplexityFailed, message, HttpStatusCode.BadRequest)
    {
    }
}
