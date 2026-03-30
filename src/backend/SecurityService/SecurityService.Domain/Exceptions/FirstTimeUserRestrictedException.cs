using System.Net;

namespace SecurityService.Domain.Exceptions;

public class FirstTimeUserRestrictedException : DomainException
{
    public FirstTimeUserRestrictedException(string message = "First-time users must change their password before accessing other resources.")
        : base(ErrorCodes.FirstTimeUserRestrictedValue, ErrorCodes.FirstTimeUserRestricted, message, HttpStatusCode.Forbidden)
    {
    }
}
