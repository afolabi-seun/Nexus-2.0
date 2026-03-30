using System.Net;

namespace UtilityService.Domain.Exceptions;

public class InvalidChannelException : DomainException
{
    public InvalidChannelException(string channel)
        : base(ErrorCodes.InvalidChannelValue, ErrorCodes.InvalidChannel, $"Invalid notification channel '{channel}'.", HttpStatusCode.BadRequest) { }
}
