using System.Net;

namespace WorkService.Domain.Exceptions;

public class ProjectKeyDuplicateException : DomainException
{
    public ProjectKeyDuplicateException(string projectKey)
        : base(ErrorCodes.ProjectKeyDuplicateValue, ErrorCodes.ProjectKeyDuplicate,
            $"A project with key '{projectKey}' already exists.", HttpStatusCode.Conflict) { }
}
