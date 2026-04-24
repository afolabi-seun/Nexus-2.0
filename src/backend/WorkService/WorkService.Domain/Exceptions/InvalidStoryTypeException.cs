namespace WorkService.Domain.Exceptions;

public class InvalidStoryTypeException : DomainException
{
    public InvalidStoryTypeException(string storyType)
        : base(ErrorCodes.InvalidStoryTypeValue, ErrorCodes.InvalidStoryType,
            $"Story type '{storyType}' is not valid. Must be Feature, Bug, Improvement, Epic, or Task.") { }
}
