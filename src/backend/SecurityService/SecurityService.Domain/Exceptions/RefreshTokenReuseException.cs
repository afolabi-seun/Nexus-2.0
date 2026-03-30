using System.Net;

namespace SecurityService.Domain.Exceptions;

public class RefreshTokenReuseException : DomainException
{
    public RefreshTokenReuseException(string message = "Refresh token reuse detected. All sessions have been revoked.")
        : base(ErrorCodes.RefreshTokenReuseValue, ErrorCodes.RefreshTokenReuse, message, HttpStatusCode.Unauthorized)
    {
    }
}
