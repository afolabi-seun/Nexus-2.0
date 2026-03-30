using System.Net;

namespace UtilityService.Domain.Exceptions;

public class ErrorCodeNotFoundException : DomainException
{
    public ErrorCodeNotFoundException(string code)
        : base(ErrorCodes.ErrorCodeNotFoundValue, ErrorCodes.ErrorCodeNotFound, $"Error code '{code}' was not found.", HttpStatusCode.NotFound) { }
}
