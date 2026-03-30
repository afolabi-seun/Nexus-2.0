using System.Net;

namespace WorkService.Domain.Exceptions;

public class InsufficientPermissionsException : DomainException
{
    public InsufficientPermissionsException()
        : base(ErrorCodes.InsufficientPermissionsValue, ErrorCodes.InsufficientPermissions,
            "You do not have sufficient permissions to perform this action.", HttpStatusCode.Forbidden) { }
}
