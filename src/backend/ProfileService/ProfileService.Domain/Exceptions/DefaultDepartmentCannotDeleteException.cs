using System.Net;

namespace ProfileService.Domain.Exceptions;

public class DefaultDepartmentCannotDeleteException : DomainException
{
    public DefaultDepartmentCannotDeleteException(string message = "Default departments cannot be deleted.")
        : base(ErrorCodes.DefaultDepartmentCannotDeleteValue, ErrorCodes.DefaultDepartmentCannotDelete, message, HttpStatusCode.BadRequest)
    {
    }
}
