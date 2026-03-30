using Microsoft.AspNetCore.Http;

namespace SecurityService.Infrastructure.Services.ServiceClients;

public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.Items.TryGetValue("CorrelationId", out var correlationId) == true
            && correlationId is string correlationIdStr
            && !string.IsNullOrEmpty(correlationIdStr))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationIdStr);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
