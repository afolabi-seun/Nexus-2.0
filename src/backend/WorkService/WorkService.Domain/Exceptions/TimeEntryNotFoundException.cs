using System.Net;

namespace WorkService.Domain.Exceptions;

public class TimeEntryNotFoundException : DomainException
{
    public TimeEntryNotFoundException(Guid timeEntryId)
        : base(ErrorCodes.TimeEntryNotFoundValue, ErrorCodes.TimeEntryNotFound,
            $"Time entry with ID '{timeEntryId}' was not found.", HttpStatusCode.NotFound) { }
}
