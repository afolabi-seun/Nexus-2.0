namespace WorkService.Domain.Exceptions;

public class InvalidMitigationStatusException : DomainException
{
    public InvalidMitigationStatusException(string status)
        : base(ErrorCodes.InvalidMitigationStatusValue, ErrorCodes.InvalidMitigationStatus,
            $"Invalid mitigation status: '{status}'. Must be one of: Open, Mitigating, Mitigated, Accepted.") { }
}
