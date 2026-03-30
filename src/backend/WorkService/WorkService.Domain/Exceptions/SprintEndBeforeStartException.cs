namespace WorkService.Domain.Exceptions;

public class SprintEndBeforeStartException : DomainException
{
    public SprintEndBeforeStartException()
        : base(ErrorCodes.SprintEndBeforeStartValue, ErrorCodes.SprintEndBeforeStart,
            "Sprint end date must be after start date.") { }
}
