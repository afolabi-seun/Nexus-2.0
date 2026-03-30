using System.Net;

namespace ProfileService.Domain.Exceptions;

public class InvalidPreferenceValueException : DomainException
{
    public InvalidPreferenceValueException(string message = "The provided preference value is invalid.")
        : base(ErrorCodes.InvalidPreferenceValueValue, ErrorCodes.InvalidPreferenceValue, message, HttpStatusCode.BadRequest)
    {
    }
}
