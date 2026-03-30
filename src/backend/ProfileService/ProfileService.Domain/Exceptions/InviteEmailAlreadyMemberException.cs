using System.Net;

namespace ProfileService.Domain.Exceptions;

public class InviteEmailAlreadyMemberException : DomainException
{
    public InviteEmailAlreadyMemberException(string message = "The invitee's email is already registered as a member in this organization.")
        : base(ErrorCodes.InviteEmailAlreadyMemberValue, ErrorCodes.InviteEmailAlreadyMember, message, HttpStatusCode.Conflict)
    {
    }
}
