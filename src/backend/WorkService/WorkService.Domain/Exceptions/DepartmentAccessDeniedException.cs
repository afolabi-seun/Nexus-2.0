using System.Net;

namespace WorkService.Domain.Exceptions;

public class DepartmentAccessDeniedException : DomainException
{
    public DepartmentAccessDeniedException()
        : base(ErrorCodes.DepartmentAccessDeniedValue, ErrorCodes.DepartmentAccessDenied,
            "You do not have access to this department.", HttpStatusCode.Forbidden) { }
}
