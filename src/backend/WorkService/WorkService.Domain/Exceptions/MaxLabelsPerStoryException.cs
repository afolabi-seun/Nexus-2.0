namespace WorkService.Domain.Exceptions;

public class MaxLabelsPerStoryException : DomainException
{
    public MaxLabelsPerStoryException(Guid storyId)
        : base(ErrorCodes.MaxLabelsPerStoryValue, ErrorCodes.MaxLabelsPerStory,
            $"Story '{storyId}' has reached the maximum of 10 labels.") { }
}
