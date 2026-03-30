using System.Net;

namespace WorkService.Domain.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(ErrorCodes.ConflictValue, ErrorCodes.Conflict,
            message, HttpStatusCode.Conflict) { }
}
