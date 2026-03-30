using System.Net;

namespace WorkService.Domain.Exceptions;

public class SprintNotFoundException : DomainException
{
    public SprintNotFoundException(Guid sprintId)
        : base(ErrorCodes.SprintNotFoundValue, ErrorCodes.SprintNotFound,
            $"Sprint with ID '{sprintId}' was not found.", HttpStatusCode.NotFound) { }
}
