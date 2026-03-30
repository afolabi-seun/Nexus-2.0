using System.Net;

namespace ProfileService.Domain.Exceptions;

public class LastOrgAdminCannotDeactivateException : DomainException
{
    public LastOrgAdminCannotDeactivateException(string message = "Cannot deactivate the last OrgAdmin in the organization.")
        : base(ErrorCodes.LastOrgAdminCannotDeactivateValue, ErrorCodes.LastOrgAdminCannotDeactivate, message, HttpStatusCode.BadRequest)
    {
    }
}
