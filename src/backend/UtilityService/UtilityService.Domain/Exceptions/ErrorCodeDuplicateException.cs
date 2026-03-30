using System.Net;

namespace UtilityService.Domain.Exceptions;

public class ErrorCodeDuplicateException : DomainException
{
    public ErrorCodeDuplicateException(string code)
        : base(ErrorCodes.ErrorCodeDuplicateValue, ErrorCodes.ErrorCodeDuplicate, $"Error code '{code}' already exists.", HttpStatusCode.Conflict) { }
}
