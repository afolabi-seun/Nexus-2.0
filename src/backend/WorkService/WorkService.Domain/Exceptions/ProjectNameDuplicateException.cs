using System.Net;

namespace WorkService.Domain.Exceptions;

public class ProjectNameDuplicateException : DomainException
{
    public ProjectNameDuplicateException(string projectName)
        : base(ErrorCodes.ProjectNameDuplicateValue, ErrorCodes.ProjectNameDuplicate,
            $"A project with name '{projectName}' already exists in this organization.", HttpStatusCode.Conflict) { }
}
