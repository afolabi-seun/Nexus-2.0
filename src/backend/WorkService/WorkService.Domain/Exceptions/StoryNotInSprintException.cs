namespace WorkService.Domain.Exceptions;

public class StoryNotInSprintException : DomainException
{
    public StoryNotInSprintException(Guid storyId, Guid sprintId)
        : base(ErrorCodes.StoryNotInSprintValue, ErrorCodes.StoryNotInSprint,
            $"Story '{storyId}' is not in sprint '{sprintId}'.") { }
}
