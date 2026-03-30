using System.Net;

namespace SecurityService.Domain.Exceptions;

public class OtpMaxAttemptsException : DomainException
{
    public OtpMaxAttemptsException(string message = "Maximum OTP verification attempts exceeded.")
        : base(ErrorCodes.OtpMaxAttemptsValue, ErrorCodes.OtpMaxAttempts, message, (HttpStatusCode)429)
    {
    }
}
