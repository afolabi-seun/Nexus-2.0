namespace WorkService.Domain.Exceptions;

public class AssigneeAtCapacityException : DomainException
{
    public AssigneeAtCapacityException(Guid assigneeId)
        : base(ErrorCodes.AssigneeAtCapacityValue, ErrorCodes.AssigneeAtCapacity,
            $"Assignee '{assigneeId}' has reached their maximum concurrent task limit.") { }
}
