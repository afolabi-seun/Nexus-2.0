namespace BillingService.Domain.Exceptions;

public class InvalidUpgradePathException : DomainException
{
    public InvalidUpgradePathException()
        : base(ErrorCodes.InvalidUpgradePathValue, ErrorCodes.InvalidUpgradePath,
            "Target plan is not a higher tier.") { }
}
