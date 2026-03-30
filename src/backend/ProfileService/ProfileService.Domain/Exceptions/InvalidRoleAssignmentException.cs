using System.Net;

namespace ProfileService.Domain.Exceptions;

public class InvalidRoleAssignmentException : DomainException
{
    public InvalidRoleAssignmentException(string message = "The role assignment is invalid.")
        : base(ErrorCodes.InvalidRoleAssignmentValue, ErrorCodes.InvalidRoleAssignment, message, HttpStatusCode.BadRequest)
    {
    }
}
