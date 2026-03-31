namespace WorkService.Domain.Exceptions;

public class DailyHoursExceededException : DomainException
{
    public DailyHoursExceededException(decimal maxDailyHours)
        : base(ErrorCodes.DailyHoursExceededValue, ErrorCodes.DailyHoursExceeded,
            $"Daily hours limit of {maxDailyHours} hours has been exceeded.") { }
}
