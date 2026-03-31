using System.Net;

namespace WorkService.Domain.Exceptions;

public class TimerAlreadyActiveException : DomainException
{
    public TimerAlreadyActiveException(Guid userId)
        : base(ErrorCodes.TimerAlreadyActiveValue, ErrorCodes.TimerAlreadyActive,
            $"User '{userId}' already has an active timer.", HttpStatusCode.Conflict) { }
}
