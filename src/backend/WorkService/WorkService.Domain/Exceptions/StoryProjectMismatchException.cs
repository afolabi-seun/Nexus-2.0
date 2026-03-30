namespace WorkService.Domain.Exceptions;

public class StoryProjectMismatchException : DomainException
{
    public StoryProjectMismatchException(Guid storyId, Guid projectId)
        : base(ErrorCodes.StoryProjectMismatchValue, ErrorCodes.StoryProjectMismatch,
            $"Story '{storyId}' does not belong to project '{projectId}'.") { }
}
