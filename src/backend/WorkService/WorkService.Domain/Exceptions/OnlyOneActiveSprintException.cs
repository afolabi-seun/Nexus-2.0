namespace WorkService.Domain.Exceptions;

public class OnlyOneActiveSprintException : DomainException
{
    public OnlyOneActiveSprintException(Guid projectId)
        : base(ErrorCodes.OnlyOneActiveSprintValue, ErrorCodes.OnlyOneActiveSprint,
            $"Project '{projectId}' already has an active sprint.") { }
}
