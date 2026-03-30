using UtilityService.Domain.Exceptions;
using UtilityService.Domain.Helpers;
using UtilityService.Domain.Interfaces.Services;

namespace UtilityService.Infrastructure.Services.Notifications;

public class TemplateRenderer : ITemplateRenderer
{
    private readonly string _templateBasePath;

    public TemplateRenderer()
    {
        _templateBasePath = Path.Combine(AppContext.BaseDirectory, "Templates");
    }

    public TemplateRenderer(string templateBasePath)
    {
        _templateBasePath = templateBasePath;
    }

    public string Render(string notificationType, string channel, Dictionary<string, string> templateVariables)
    {
        var fileName = ToKebabCase(notificationType);
        string templatePath;

        if (channel == NotificationChannels.Email)
            templatePath = Path.Combine(_templateBasePath, "Email", $"{fileName}.html");
        else
            templatePath = Path.Combine(_templateBasePath, "Push", $"{fileName}.txt");

        if (!File.Exists(templatePath))
            throw new TemplateNotFoundException($"{channel}/{fileName}");

        var template = File.ReadAllText(templatePath);

        foreach (var (key, value) in templateVariables)
        {
            template = template.Replace($"{{{{{key}}}}}", value);
        }

        return template;
    }

    private static string ToKebabCase(string input)
    {
        return string.Concat(input.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? $"-{char.ToLower(c)}" : $"{char.ToLower(c)}"));
    }
}
