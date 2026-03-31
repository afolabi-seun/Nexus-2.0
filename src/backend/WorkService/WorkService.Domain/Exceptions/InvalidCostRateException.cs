namespace WorkService.Domain.Exceptions;

public class InvalidCostRateException : DomainException
{
    public InvalidCostRateException(string message)
        : base(ErrorCodes.InvalidCostRateValue, ErrorCodes.InvalidCostRate,
            message) { }
}
