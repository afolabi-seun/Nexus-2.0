namespace WorkService.Domain.Exceptions;

public class InvalidTimePolicyException : DomainException
{
    public InvalidTimePolicyException(string message)
        : base(ErrorCodes.InvalidTimePolicyValue, ErrorCodes.InvalidTimePolicy,
            message) { }
}
