using System.Net;

namespace SecurityService.Domain.Exceptions;

public class OtpVerificationFailedException : DomainException
{
    public OtpVerificationFailedException(string message = "OTP verification failed.")
        : base(ErrorCodes.OtpVerificationFailedValue, ErrorCodes.OtpVerificationFailed, message, HttpStatusCode.BadRequest)
    {
    }
}
