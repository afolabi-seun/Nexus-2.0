using System.Net;

namespace ProfileService.Domain.Exceptions;

public class StoryPrefixDuplicateException : DomainException
{
    public StoryPrefixDuplicateException(string message = "This story ID prefix is already in use by another organization.")
        : base(ErrorCodes.StoryPrefixDuplicateValue, ErrorCodes.StoryPrefixDuplicate, message, HttpStatusCode.Conflict)
    {
    }
}
