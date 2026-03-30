using System.Net;

namespace SecurityService.Domain.Exceptions;

public class OrganizationMismatchException : DomainException
{
    public OrganizationMismatchException(string message = "Organization mismatch. Cross-organization access is not permitted.")
        : base(ErrorCodes.OrganizationMismatchValue, ErrorCodes.OrganizationMismatch, message, HttpStatusCode.Forbidden)
    {
    }
}
