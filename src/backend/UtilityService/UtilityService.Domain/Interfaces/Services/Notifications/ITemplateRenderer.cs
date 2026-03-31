namespace UtilityService.Domain.Interfaces.Services.Notifications;

public interface ITemplateRenderer
{
    string Render(string notificationType, string channel, Dictionary<string, string> templateVariables);
}
