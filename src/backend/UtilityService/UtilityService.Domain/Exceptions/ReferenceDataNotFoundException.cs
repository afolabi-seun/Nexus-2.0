using System.Net;

namespace UtilityService.Domain.Exceptions;

public class ReferenceDataNotFoundException : DomainException
{
    public ReferenceDataNotFoundException(string message = "Reference data not found.")
        : base(ErrorCodes.ReferenceDataNotFoundValue, ErrorCodes.ReferenceDataNotFound, message, HttpStatusCode.NotFound) { }
}
