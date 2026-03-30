using System.Net;

namespace ProfileService.Domain.Exceptions;

public class DepartmentCodeDuplicateException : DomainException
{
    public DepartmentCodeDuplicateException(string message = "A department with this code already exists in the organization.")
        : base(ErrorCodes.DepartmentCodeDuplicateValue, ErrorCodes.DepartmentCodeDuplicate, message, HttpStatusCode.Conflict)
    {
    }
}
