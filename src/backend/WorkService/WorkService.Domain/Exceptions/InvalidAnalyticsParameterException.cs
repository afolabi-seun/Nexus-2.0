namespace WorkService.Domain.Exceptions;

public class InvalidAnalyticsParameterException : DomainException
{
    public InvalidAnalyticsParameterException(string message)
        : base(ErrorCodes.InvalidAnalyticsParameterValue, ErrorCodes.InvalidAnalyticsParameter,
            message) { }
}
