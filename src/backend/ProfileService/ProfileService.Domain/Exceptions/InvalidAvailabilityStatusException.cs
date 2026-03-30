using System.Net;

namespace ProfileService.Domain.Exceptions;

public class InvalidAvailabilityStatusException : DomainException
{
    public InvalidAvailabilityStatusException(string message = "Invalid availability status. Must be one of: Available, Busy, Away, Offline.")
        : base(ErrorCodes.InvalidAvailabilityStatusValue, ErrorCodes.InvalidAvailabilityStatus, message, HttpStatusCode.BadRequest)
    {
    }
}
