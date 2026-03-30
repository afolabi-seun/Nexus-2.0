using System.Net;

namespace WorkService.Domain.Exceptions;

public class StoryAlreadyInSprintException : DomainException
{
    public StoryAlreadyInSprintException(Guid storyId, Guid sprintId)
        : base(ErrorCodes.StoryAlreadyInSprintValue, ErrorCodes.StoryAlreadyInSprint,
            $"Story '{storyId}' is already in sprint '{sprintId}'.", HttpStatusCode.Conflict) { }
}
