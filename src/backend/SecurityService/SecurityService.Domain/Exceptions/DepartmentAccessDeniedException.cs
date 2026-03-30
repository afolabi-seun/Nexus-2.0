using System.Net;

namespace SecurityService.Domain.Exceptions;

public class DepartmentAccessDeniedException : DomainException
{
    public DepartmentAccessDeniedException(string message = "Access denied for the specified department.")
        : base(ErrorCodes.DepartmentAccessDeniedValue, ErrorCodes.DepartmentAccessDenied, message, HttpStatusCode.Forbidden)
    {
    }
}
