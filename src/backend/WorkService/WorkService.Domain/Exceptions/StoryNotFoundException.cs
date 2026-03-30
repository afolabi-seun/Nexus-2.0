using System.Net;

namespace WorkService.Domain.Exceptions;

public class StoryNotFoundException : DomainException
{
    public StoryNotFoundException(Guid storyId)
        : base(ErrorCodes.StoryNotFoundValue, ErrorCodes.StoryNotFound,
            $"Story with ID '{storyId}' was not found.", HttpStatusCode.NotFound) { }
}
