namespace SecurityService.Domain.Interfaces.Services.RateLimiter;

public interface IRateLimiterService
{
    Task<(bool IsAllowed, int RetryAfterSeconds)> CheckRateLimitAsync(string identity, string endpoint, int maxRequests, TimeSpan window, CancellationToken ct = default);
}
