using System.Net;

namespace WorkService.Domain.Exceptions;

public class LabelNotFoundException : DomainException
{
    public LabelNotFoundException(Guid labelId)
        : base(ErrorCodes.LabelNotFoundValue, ErrorCodes.LabelNotFound,
            $"Label with ID '{labelId}' was not found.", HttpStatusCode.NotFound) { }
}
