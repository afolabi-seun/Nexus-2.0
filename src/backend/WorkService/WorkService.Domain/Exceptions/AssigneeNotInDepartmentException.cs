namespace WorkService.Domain.Exceptions;

public class AssigneeNotInDepartmentException : DomainException
{
    public AssigneeNotInDepartmentException(Guid assigneeId, string department)
        : base(ErrorCodes.AssigneeNotInDepartmentValue, ErrorCodes.AssigneeNotInDepartment,
            $"Assignee '{assigneeId}' is not a member of department '{department}'.") { }
}
