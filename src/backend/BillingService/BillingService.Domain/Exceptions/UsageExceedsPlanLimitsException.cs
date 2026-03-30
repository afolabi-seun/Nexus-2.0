namespace BillingService.Domain.Exceptions;

public class UsageExceedsPlanLimitsException : DomainException
{
    public UsageExceedsPlanLimitsException(string details)
        : base(ErrorCodes.UsageExceedsPlanLimitsValue, ErrorCodes.UsageExceedsPlanLimits,
            $"Current usage exceeds target plan limits. {details}") { }
}
