using System.Net;

namespace ProfileService.Domain.Exceptions;

public class DepartmentHasActiveMembersException : DomainException
{
    public DepartmentHasActiveMembersException(string message = "Cannot deactivate a department that has active members.")
        : base(ErrorCodes.DepartmentHasActiveMembersValue, ErrorCodes.DepartmentHasActiveMembers, message, HttpStatusCode.BadRequest)
    {
    }
}
