namespace WorkService.Domain.Exceptions;

public class InvalidRiskLikelihoodException : DomainException
{
    public InvalidRiskLikelihoodException(string likelihood)
        : base(ErrorCodes.InvalidRiskLikelihoodValue, ErrorCodes.InvalidRiskLikelihood,
            $"Invalid risk likelihood: '{likelihood}'. Must be one of: Low, Medium, High.") { }
}
