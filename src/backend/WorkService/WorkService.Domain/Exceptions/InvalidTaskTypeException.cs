namespace WorkService.Domain.Exceptions;

public class InvalidTaskTypeException : DomainException
{
    public InvalidTaskTypeException(string taskType)
        : base(ErrorCodes.InvalidTaskTypeValue, ErrorCodes.InvalidTaskType,
            $"Task type '{taskType}' is not valid. Must be Development, Testing, DevOps, Design, Documentation, or Bug.") { }
}
