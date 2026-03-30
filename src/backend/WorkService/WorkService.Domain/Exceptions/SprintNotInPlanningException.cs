namespace WorkService.Domain.Exceptions;

public class SprintNotInPlanningException : DomainException
{
    public SprintNotInPlanningException(Guid sprintId)
        : base(ErrorCodes.SprintNotInPlanningValue, ErrorCodes.SprintNotInPlanning,
            $"Sprint '{sprintId}' is not in Planning status.") { }
}
