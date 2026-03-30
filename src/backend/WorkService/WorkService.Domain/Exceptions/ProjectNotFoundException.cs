using System.Net;

namespace WorkService.Domain.Exceptions;

public class ProjectNotFoundException : DomainException
{
    public ProjectNotFoundException(Guid projectId)
        : base(ErrorCodes.ProjectNotFoundValue, ErrorCodes.ProjectNotFound,
            $"Project with ID '{projectId}' was not found.", HttpStatusCode.NotFound) { }
}
