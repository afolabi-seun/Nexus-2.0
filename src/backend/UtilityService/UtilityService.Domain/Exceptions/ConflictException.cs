using System.Net;

namespace UtilityService.Domain.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(string message = "A conflict occurred.")
        : base(ErrorCodes.ConflictValue, ErrorCodes.Conflict, message, HttpStatusCode.Conflict) { }
}
