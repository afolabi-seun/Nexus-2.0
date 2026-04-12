using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProfileService.Api.Extensions;

public class HideServiceAuthFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var description in context.ApiDescriptions)
        {
            var hasServiceAuth = description.CustomAttributes()
                .Any(a => a.GetType().Name == "ServiceAuthAttribute");

            if (!hasServiceAuth) continue;

            var route = "/" + description.RelativePath?.TrimEnd('/');
            if (swaggerDoc.Paths.ContainsKey(route))
                swaggerDoc.Paths.Remove(route);
        }
    }
}
