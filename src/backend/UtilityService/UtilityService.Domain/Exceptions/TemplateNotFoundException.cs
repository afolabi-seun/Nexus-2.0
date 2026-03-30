using System.Net;

namespace UtilityService.Domain.Exceptions;

public class TemplateNotFoundException : DomainException
{
    public TemplateNotFoundException(string templateName)
        : base(ErrorCodes.TemplateNotFoundValue, ErrorCodes.TemplateNotFound, $"Template '{templateName}' was not found.", HttpStatusCode.NotFound) { }
}
