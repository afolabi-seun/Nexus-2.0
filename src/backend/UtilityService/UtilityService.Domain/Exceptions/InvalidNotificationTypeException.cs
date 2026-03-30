using System.Net;

namespace UtilityService.Domain.Exceptions;

public class InvalidNotificationTypeException : DomainException
{
    public InvalidNotificationTypeException(string notificationType)
        : base(ErrorCodes.InvalidNotificationTypeValue, ErrorCodes.InvalidNotificationType, $"Invalid notification type '{notificationType}'.", HttpStatusCode.BadRequest) { }
}
