using System.Net;

namespace SecurityService.Domain.Exceptions;

public class RateLimitExceededException : DomainException
{
    public int RetryAfterSeconds { get; }

    public RateLimitExceededException(int retryAfterSeconds, string message = "Rate limit exceeded.")
        : base(ErrorCodes.RateLimitExceededValue, ErrorCodes.RateLimitExceeded, message, (HttpStatusCode)429)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
