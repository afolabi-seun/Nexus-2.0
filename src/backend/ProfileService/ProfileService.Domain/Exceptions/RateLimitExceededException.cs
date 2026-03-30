using System.Net;

namespace ProfileService.Domain.Exceptions;

public class RateLimitExceededException : DomainException
{
    public int RetryAfterSeconds { get; }

    public RateLimitExceededException(int retryAfterSeconds = 60, string message = "Rate limit exceeded. Please try again later.")
        : base(ErrorCodes.RateLimitExceededValue, ErrorCodes.RateLimitExceeded, message, (HttpStatusCode)429)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
