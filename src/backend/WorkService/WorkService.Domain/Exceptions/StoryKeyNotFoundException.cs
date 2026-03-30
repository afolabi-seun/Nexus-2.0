using System.Net;

namespace WorkService.Domain.Exceptions;

public class StoryKeyNotFoundException : DomainException
{
    public StoryKeyNotFoundException(string storyKey)
        : base(ErrorCodes.StoryKeyNotFoundValue, ErrorCodes.StoryKeyNotFound,
            $"Story with key '{storyKey}' was not found.", HttpStatusCode.NotFound) { }
}
