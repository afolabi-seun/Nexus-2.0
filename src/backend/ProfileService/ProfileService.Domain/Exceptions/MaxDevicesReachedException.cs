using System.Net;

namespace ProfileService.Domain.Exceptions;

public class MaxDevicesReachedException : DomainException
{
    public MaxDevicesReachedException(string message = "Maximum number of devices has been reached.")
        : base(ErrorCodes.MaxDevicesReachedValue, ErrorCodes.MaxDevicesReached, message, HttpStatusCode.BadRequest)
    {
    }
}
