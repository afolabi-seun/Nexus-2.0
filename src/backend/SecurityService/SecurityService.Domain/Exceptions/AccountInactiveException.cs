using System.Net;

namespace SecurityService.Domain.Exceptions;

public class AccountInactiveException : DomainException
{
    public AccountInactiveException(string message = "Account is inactive.")
        : base(ErrorCodes.AccountInactiveValue, ErrorCodes.AccountInactive, message, HttpStatusCode.Forbidden)
    {
    }
}
