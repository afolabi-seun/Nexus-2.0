using System.Net;

namespace SecurityService.Domain.Exceptions;

public class AccountLockedException : DomainException
{
    public AccountLockedException(string message = "Account is locked due to too many failed login attempts.")
        : base(ErrorCodes.AccountLockedValue, ErrorCodes.AccountLocked, message, (HttpStatusCode)423)
    {
    }
}
