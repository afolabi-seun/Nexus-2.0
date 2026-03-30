namespace WorkService.Domain.Exceptions;

public class StoryInActiveSprintException : DomainException
{
    public StoryInActiveSprintException(Guid storyId)
        : base(ErrorCodes.StoryInActiveSprintValue, ErrorCodes.StoryInActiveSprint,
            $"Story '{storyId}' is in an active sprint and cannot be deleted.") { }
}
