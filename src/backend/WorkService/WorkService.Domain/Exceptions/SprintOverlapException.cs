namespace WorkService.Domain.Exceptions;

public class SprintOverlapException : DomainException
{
    public SprintOverlapException()
        : base(ErrorCodes.SprintOverlapValue, ErrorCodes.SprintOverlap,
            "Sprint dates overlap with an existing sprint.") { }
}
