namespace WorkService.Domain.Exceptions;

public class InvalidStoryTransitionException : DomainException
{
    public InvalidStoryTransitionException(string from, string to)
        : base(ErrorCodes.InvalidStoryTransitionValue, ErrorCodes.InvalidStoryTransition,
            $"Invalid story status transition from '{from}' to '{to}'.") { }
}
