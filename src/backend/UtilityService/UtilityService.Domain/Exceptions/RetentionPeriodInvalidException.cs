using System.Net;

namespace UtilityService.Domain.Exceptions;

public class RetentionPeriodInvalidException : DomainException
{
    public RetentionPeriodInvalidException(string message = "Retention period must be greater than zero.")
        : base(ErrorCodes.RetentionPeriodInvalidValue, ErrorCodes.RetentionPeriodInvalid, message, HttpStatusCode.BadRequest) { }
}
