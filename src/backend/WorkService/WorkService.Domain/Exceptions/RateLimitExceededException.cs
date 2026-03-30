using System.Net;

namespace WorkService.Domain.Exceptions;

public class RateLimitExceededException : DomainException
{
    public int RetryAfterSeconds { get; }

    public RateLimitExceededException(int retryAfterSeconds = 60)
        : base(429, "RATE_LIMIT_EXCEEDED",
            $"Rate limit exceeded. Retry after {retryAfterSeconds} seconds.",
            (HttpStatusCode)429)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
