using System.Net;

namespace UtilityService.Domain.Exceptions;

public class OrganizationMismatchException : DomainException
{
    public OrganizationMismatchException(string message = "Organization mismatch. Access denied.")
        : base(ErrorCodes.OrganizationMismatchValue, ErrorCodes.OrganizationMismatch, message, HttpStatusCode.Forbidden) { }
}
