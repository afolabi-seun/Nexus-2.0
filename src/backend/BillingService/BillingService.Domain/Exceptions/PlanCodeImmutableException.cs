using System.Net;

namespace BillingService.Domain.Exceptions;

public class PlanCodeImmutableException : DomainException
{
    public PlanCodeImmutableException()
        : base(ErrorCodes.PlanCodeImmutableValue, ErrorCodes.PlanCodeImmutable,
            "The plan code cannot be changed after creation.", HttpStatusCode.BadRequest) { }
}
