namespace WorkService.Domain.Exceptions;

public class SprintAlreadyActiveException : DomainException
{
    public SprintAlreadyActiveException(Guid sprintId)
        : base(ErrorCodes.SprintAlreadyActiveValue, ErrorCodes.SprintAlreadyActive,
            $"Sprint '{sprintId}' is already active.") { }
}
