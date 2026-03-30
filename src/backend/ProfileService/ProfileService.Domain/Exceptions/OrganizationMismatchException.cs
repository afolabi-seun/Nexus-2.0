using System.Net;

namespace ProfileService.Domain.Exceptions;

public class OrganizationMismatchException : DomainException
{
    public OrganizationMismatchException(string message = "Organization mismatch. Access denied.")
        : base(ErrorCodes.OrganizationMismatchValue, ErrorCodes.OrganizationMismatch, message, HttpStatusCode.Forbidden)
    {
    }
}
