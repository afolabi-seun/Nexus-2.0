using System.Net;

namespace WorkService.Domain.Exceptions;

public class StorySequenceInitFailedException : DomainException
{
    public StorySequenceInitFailedException(Guid projectId)
        : base(ErrorCodes.StorySequenceInitFailedValue, ErrorCodes.StorySequenceInitFailed,
            $"Failed to initialize story sequence for project '{projectId}'.",
            HttpStatusCode.InternalServerError) { }
}
