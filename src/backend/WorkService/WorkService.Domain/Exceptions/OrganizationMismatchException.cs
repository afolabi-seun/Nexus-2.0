using System.Net;

namespace WorkService.Domain.Exceptions;

public class OrganizationMismatchException : DomainException
{
    public OrganizationMismatchException()
        : base(ErrorCodes.OrganizationMismatchValue, ErrorCodes.OrganizationMismatch,
            "The entity does not belong to the current organization.", HttpStatusCode.Forbidden) { }
}
