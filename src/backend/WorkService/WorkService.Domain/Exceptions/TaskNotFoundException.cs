using System.Net;

namespace WorkService.Domain.Exceptions;

public class TaskNotFoundException : DomainException
{
    public TaskNotFoundException(Guid taskId)
        : base(ErrorCodes.TaskNotFoundValue, ErrorCodes.TaskNotFound,
            $"Task with ID '{taskId}' was not found.", HttpStatusCode.NotFound) { }
}
