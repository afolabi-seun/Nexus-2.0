namespace WorkService.Domain.Exceptions;

public class ProjectKeyInvalidFormatException : DomainException
{
    public ProjectKeyInvalidFormatException(string projectKey)
        : base(ErrorCodes.ProjectKeyInvalidFormatValue, ErrorCodes.ProjectKeyInvalidFormat,
            $"Project key '{projectKey}' is invalid. Must be 2–10 uppercase alphanumeric characters.") { }
}
