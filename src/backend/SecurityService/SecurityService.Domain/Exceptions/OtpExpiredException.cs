using System.Net;

namespace SecurityService.Domain.Exceptions;

public class OtpExpiredException : DomainException
{
    public OtpExpiredException(string message = "OTP has expired.")
        : base(ErrorCodes.OtpExpiredValue, ErrorCodes.OtpExpired, message, HttpStatusCode.BadRequest)
    {
    }
}
