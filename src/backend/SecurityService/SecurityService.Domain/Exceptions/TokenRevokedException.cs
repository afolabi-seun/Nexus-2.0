using System.Net;

namespace SecurityService.Domain.Exceptions;

public class TokenRevokedException : DomainException
{
    public TokenRevokedException(string message = "Token has been revoked.")
        : base(ErrorCodes.TokenRevokedValue, ErrorCodes.TokenRevoked, message, HttpStatusCode.Unauthorized)
    {
    }
}
