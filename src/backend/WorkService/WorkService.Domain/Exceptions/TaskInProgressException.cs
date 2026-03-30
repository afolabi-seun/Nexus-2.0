namespace WorkService.Domain.Exceptions;

public class TaskInProgressException : DomainException
{
    public TaskInProgressException(Guid taskId)
        : base(ErrorCodes.TaskInProgressValue, ErrorCodes.TaskInProgress,
            $"Task '{taskId}' is in progress and cannot be deleted.") { }
}
