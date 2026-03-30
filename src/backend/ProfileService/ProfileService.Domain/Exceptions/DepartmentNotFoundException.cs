using System.Net;

namespace ProfileService.Domain.Exceptions;

public class DepartmentNotFoundException : DomainException
{
    public DepartmentNotFoundException(string message = "The requested department was not found.")
        : base(ErrorCodes.DepartmentNotFoundValue, ErrorCodes.DepartmentNotFound, message, HttpStatusCode.NotFound)
    {
    }
}
