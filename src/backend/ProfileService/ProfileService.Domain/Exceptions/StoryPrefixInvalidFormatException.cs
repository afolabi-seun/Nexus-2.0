using System.Net;

namespace ProfileService.Domain.Exceptions;

public class StoryPrefixInvalidFormatException : DomainException
{
    public StoryPrefixInvalidFormatException(string message = "StoryIdPrefix must be 2-10 uppercase alphanumeric characters.")
        : base(ErrorCodes.StoryPrefixInvalidFormatValue, ErrorCodes.StoryPrefixInvalidFormat, message, HttpStatusCode.BadRequest)
    {
    }
}
