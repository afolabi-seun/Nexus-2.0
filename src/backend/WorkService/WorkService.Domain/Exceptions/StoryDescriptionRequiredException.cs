namespace WorkService.Domain.Exceptions;

public class StoryDescriptionRequiredException : DomainException
{
    public StoryDescriptionRequiredException()
        : base(ErrorCodes.StoryDescriptionRequiredValue, ErrorCodes.StoryDescriptionRequired,
            "Story must have a description before transitioning to Ready.") { }
}
