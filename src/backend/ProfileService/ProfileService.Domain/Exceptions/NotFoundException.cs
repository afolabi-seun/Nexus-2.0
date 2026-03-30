using System.Net;

namespace ProfileService.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string message = "The requested resource was not found.")
        : base(ErrorCodes.NotFoundValue, ErrorCodes.NotFound, message, HttpStatusCode.NotFound)
    {
    }
}
