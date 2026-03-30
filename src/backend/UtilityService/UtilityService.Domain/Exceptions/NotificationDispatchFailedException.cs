using System.Net;

namespace UtilityService.Domain.Exceptions;

public class NotificationDispatchFailedException : DomainException
{
    public NotificationDispatchFailedException(string message = "Notification dispatch failed.")
        : base(ErrorCodes.NotificationDispatchFailedValue, ErrorCodes.NotificationDispatchFailed, message, HttpStatusCode.InternalServerError) { }
}
