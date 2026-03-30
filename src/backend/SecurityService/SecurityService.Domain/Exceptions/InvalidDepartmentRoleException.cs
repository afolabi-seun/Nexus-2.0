using System.Net;

namespace SecurityService.Domain.Exceptions;

public class InvalidDepartmentRoleException : DomainException
{
    public InvalidDepartmentRoleException(string message = "Invalid department role specified.")
        : base(ErrorCodes.InvalidDepartmentRoleValue, ErrorCodes.InvalidDepartmentRole, message, HttpStatusCode.Forbidden)
    {
    }
}
