using System.Net;

namespace ProfileService.Domain.Exceptions;

public class EmailAlreadyRegisteredException : DomainException
{
    public EmailAlreadyRegisteredException(string message = "This email address is already registered.")
        : base(ErrorCodes.EmailAlreadyRegisteredValue, ErrorCodes.EmailAlreadyRegistered, message, HttpStatusCode.Conflict)
    {
    }
}
