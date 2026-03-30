using System.Net;

namespace UtilityService.Domain.Exceptions;

public class ReferenceDataDuplicateException : DomainException
{
    public ReferenceDataDuplicateException(string message = "Reference data already exists.")
        : base(ErrorCodes.ReferenceDataDuplicateValue, ErrorCodes.ReferenceDataDuplicate, message, HttpStatusCode.Conflict) { }
}
