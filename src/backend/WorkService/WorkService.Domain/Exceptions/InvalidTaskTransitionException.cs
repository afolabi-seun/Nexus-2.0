namespace WorkService.Domain.Exceptions;

public class InvalidTaskTransitionException : DomainException
{
    public InvalidTaskTransitionException(string from, string to)
        : base(ErrorCodes.InvalidTaskTransitionValue, ErrorCodes.InvalidTaskTransition,
            $"Invalid task status transition from '{from}' to '{to}'.") { }
}
