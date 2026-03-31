namespace WorkService.Domain.Exceptions;

public class NoActiveTimerException : DomainException
{
    public NoActiveTimerException(Guid userId)
        : base(ErrorCodes.NoActiveTimerValue, ErrorCodes.NoActiveTimer,
            $"No active timer found for user '{userId}'.") { }
}
