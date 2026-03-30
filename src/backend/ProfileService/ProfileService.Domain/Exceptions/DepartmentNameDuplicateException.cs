using System.Net;

namespace ProfileService.Domain.Exceptions;

public class DepartmentNameDuplicateException : DomainException
{
    public DepartmentNameDuplicateException(string message = "A department with this name already exists in the organization.")
        : base(ErrorCodes.DepartmentNameDuplicateValue, ErrorCodes.DepartmentNameDuplicate, message, HttpStatusCode.Conflict)
    {
    }
}
