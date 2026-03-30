namespace WorkService.Domain.Exceptions;

public class StoryRequiresAssigneeException : DomainException
{
    public StoryRequiresAssigneeException()
        : base(ErrorCodes.StoryRequiresAssigneeValue, ErrorCodes.StoryRequiresAssignee,
            "Story must have an assignee before transitioning to InProgress.") { }
}
