namespace UtilityService.Domain.Interfaces.Services;

public interface ITemplateRenderer
{
    string Render(string notificationType, string channel, Dictionary<string, string> templateVariables);
}
