using System.Net;

namespace WorkService.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, Guid id)
        : base(ErrorCodes.NotFoundValue, ErrorCodes.NotFound,
            $"{entity} with ID '{id}' was not found.", HttpStatusCode.NotFound) { }
}
