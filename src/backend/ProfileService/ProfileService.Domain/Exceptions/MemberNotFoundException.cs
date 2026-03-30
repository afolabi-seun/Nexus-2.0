using System.Net;

namespace ProfileService.Domain.Exceptions;

public class MemberNotFoundException : DomainException
{
    public MemberNotFoundException(string message = "The requested team member was not found.")
        : base(ErrorCodes.MemberNotFoundValue, ErrorCodes.MemberNotFound, message, HttpStatusCode.NotFound)
    {
    }
}
