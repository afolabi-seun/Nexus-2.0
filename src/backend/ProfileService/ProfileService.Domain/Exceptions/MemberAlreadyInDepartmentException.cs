using System.Net;

namespace ProfileService.Domain.Exceptions;

public class MemberAlreadyInDepartmentException : DomainException
{
    public MemberAlreadyInDepartmentException(string message = "The member is already in this department.")
        : base(ErrorCodes.MemberAlreadyInDepartmentValue, ErrorCodes.MemberAlreadyInDepartment, message, HttpStatusCode.Conflict)
    {
    }
}
