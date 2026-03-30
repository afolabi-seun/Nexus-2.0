using System.Net;

namespace ProfileService.Domain.Exceptions;

public class PreferenceKeyUnknownException : DomainException
{
    public PreferenceKeyUnknownException(string message = "The provided preference key is unknown.")
        : base(ErrorCodes.PreferenceKeyUnknownValue, ErrorCodes.PreferenceKeyUnknown, message, HttpStatusCode.BadRequest)
    {
    }
}
