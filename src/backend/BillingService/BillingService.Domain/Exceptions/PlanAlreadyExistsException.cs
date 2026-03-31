using System.Net;

namespace BillingService.Domain.Exceptions;

public class PlanAlreadyExistsException : DomainException
{
    public PlanAlreadyExistsException()
        : base(ErrorCodes.PlanAlreadyExistsValue, ErrorCodes.PlanAlreadyExists,
            "A plan with the specified plan code already exists.", HttpStatusCode.Conflict) { }
}
