namespace WorkService.Domain.Exceptions;

public class StoryRequiresTasksException : DomainException
{
    public StoryRequiresTasksException()
        : base(ErrorCodes.StoryRequiresTasksValue, ErrorCodes.StoryRequiresTasks,
            "Story must have at least one task before transitioning to InReview.") { }
}
