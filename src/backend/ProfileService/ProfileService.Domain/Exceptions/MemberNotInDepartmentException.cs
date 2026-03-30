using System.Net;

namespace ProfileService.Domain.Exceptions;

public class MemberNotInDepartmentException : DomainException
{
    public MemberNotInDepartmentException(string message = "The member is not in this department.")
        : base(ErrorCodes.MemberNotInDepartmentValue, ErrorCodes.MemberNotInDepartment, message, HttpStatusCode.BadRequest)
    {
    }
}
