namespace BillingService.Domain.Exceptions;

public class TrialExpiredException : DomainException
{
    public TrialExpiredException()
        : base(ErrorCodes.TrialExpiredValue, ErrorCodes.TrialExpired,
            "Trial period has ended.") { }
}
