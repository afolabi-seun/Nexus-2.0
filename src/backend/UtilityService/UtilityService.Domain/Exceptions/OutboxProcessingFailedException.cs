using System.Net;

namespace UtilityService.Domain.Exceptions;

public class OutboxProcessingFailedException : DomainException
{
    public OutboxProcessingFailedException(string message = "Outbox message processing failed.")
        : base(ErrorCodes.OutboxProcessingFailedValue, ErrorCodes.OutboxProcessingFailed, message, HttpStatusCode.InternalServerError) { }
}
