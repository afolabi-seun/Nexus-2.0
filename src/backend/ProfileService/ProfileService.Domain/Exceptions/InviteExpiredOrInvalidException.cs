using System.Net;

namespace ProfileService.Domain.Exceptions;

public class InviteExpiredOrInvalidException : DomainException
{
    public InviteExpiredOrInvalidException(string message = "The invite token is expired or invalid.")
        : base(ErrorCodes.InviteExpiredOrInvalidValue, ErrorCodes.InviteExpiredOrInvalid, message, HttpStatusCode.Gone)
    {
    }
}
