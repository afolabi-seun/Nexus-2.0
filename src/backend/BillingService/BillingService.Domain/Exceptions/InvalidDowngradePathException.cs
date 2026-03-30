namespace BillingService.Domain.Exceptions;

public class InvalidDowngradePathException : DomainException
{
    public InvalidDowngradePathException()
        : base(ErrorCodes.InvalidDowngradePathValue, ErrorCodes.InvalidDowngradePath,
            "Target plan is not a lower tier.") { }
}
