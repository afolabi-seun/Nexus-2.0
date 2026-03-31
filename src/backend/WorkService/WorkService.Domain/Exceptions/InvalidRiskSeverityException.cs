namespace WorkService.Domain.Exceptions;

public class InvalidRiskSeverityException : DomainException
{
    public InvalidRiskSeverityException(string severity)
        : base(ErrorCodes.InvalidRiskSeverityValue, ErrorCodes.InvalidRiskSeverity,
            $"Invalid risk severity: '{severity}'. Must be one of: Low, Medium, High, Critical.") { }
}
