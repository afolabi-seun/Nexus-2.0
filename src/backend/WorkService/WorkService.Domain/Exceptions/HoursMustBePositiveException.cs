namespace WorkService.Domain.Exceptions;

public class HoursMustBePositiveException : DomainException
{
    public HoursMustBePositiveException()
        : base(ErrorCodes.HoursMustBePositiveValue, ErrorCodes.HoursMustBePositive,
            "Hours must be a positive value.") { }
}
