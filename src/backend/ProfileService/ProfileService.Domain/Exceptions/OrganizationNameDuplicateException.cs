using System.Net;

namespace ProfileService.Domain.Exceptions;

public class OrganizationNameDuplicateException : DomainException
{
    public OrganizationNameDuplicateException(string message = "An organization with this name already exists.")
        : base(ErrorCodes.OrganizationNameDuplicateValue, ErrorCodes.OrganizationNameDuplicate, message, HttpStatusCode.Conflict)
    {
    }
}
