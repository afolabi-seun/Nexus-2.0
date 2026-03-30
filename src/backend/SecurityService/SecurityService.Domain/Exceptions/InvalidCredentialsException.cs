using System.Net;

namespace SecurityService.Domain.Exceptions;

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException(string message = "Invalid credentials provided.")
        : base(ErrorCodes.InvalidCredentialsValue, ErrorCodes.InvalidCredentials, message, HttpStatusCode.Unauthorized)
    {
    }
}
