using System.Net;

namespace ProfileService.Domain.Exceptions;

public class MemberMustHaveDepartmentException : DomainException
{
    public MemberMustHaveDepartmentException(string message = "A member must belong to at least one department.")
        : base(ErrorCodes.MemberMustHaveDepartmentValue, ErrorCodes.MemberMustHaveDepartment, message, HttpStatusCode.BadRequest)
    {
    }
}
