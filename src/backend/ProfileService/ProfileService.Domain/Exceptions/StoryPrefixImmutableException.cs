using System.Net;

namespace ProfileService.Domain.Exceptions;

public class StoryPrefixImmutableException : DomainException
{
    public StoryPrefixImmutableException(string message = "The story ID prefix cannot be changed after stories exist.")
        : base(ErrorCodes.StoryPrefixImmutableValue, ErrorCodes.StoryPrefixImmutable, message, HttpStatusCode.BadRequest)
    {
    }
}
