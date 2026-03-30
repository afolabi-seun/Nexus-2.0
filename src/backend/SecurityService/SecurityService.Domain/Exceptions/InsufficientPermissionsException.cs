using System.Net;

namespace SecurityService.Domain.Exceptions;

public class InsufficientPermissionsException : DomainException
{
    public InsufficientPermissionsException(string message = "Insufficient permissions to perform this action.")
        : base(ErrorCodes.InsufficientPermissionsValue, ErrorCodes.InsufficientPermissions, message, HttpStatusCode.Forbidden)
    {
    }
}
