using System.Net;

namespace SecurityService.Domain.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(string message = "A conflict occurred with the current state of the resource.")
        : base(ErrorCodes.ConflictValue, ErrorCodes.Conflict, message, HttpStatusCode.Conflict)
    {
    }
}
