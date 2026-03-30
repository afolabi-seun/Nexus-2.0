namespace WorkService.Domain.Exceptions;

public class StoryRequiresPointsException : DomainException
{
    public StoryRequiresPointsException()
        : base(ErrorCodes.StoryRequiresPointsValue, ErrorCodes.StoryRequiresPoints,
            "Story must have story points before transitioning to Ready.") { }
}
