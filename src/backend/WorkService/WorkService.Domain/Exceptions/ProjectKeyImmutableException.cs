namespace WorkService.Domain.Exceptions;

public class ProjectKeyImmutableException : DomainException
{
    public ProjectKeyImmutableException(string projectKey)
        : base(ErrorCodes.ProjectKeyImmutableValue, ErrorCodes.ProjectKeyImmutable,
            $"Project key '{projectKey}' cannot be changed because stories already exist.") { }
}
