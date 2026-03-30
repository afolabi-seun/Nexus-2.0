namespace WorkService.Domain.Exceptions;

public class SprintAlreadyCompletedException : DomainException
{
    public SprintAlreadyCompletedException(Guid sprintId)
        : base(ErrorCodes.SprintAlreadyCompletedValue, ErrorCodes.SprintAlreadyCompleted,
            $"Sprint '{sprintId}' is already completed.") { }
}
