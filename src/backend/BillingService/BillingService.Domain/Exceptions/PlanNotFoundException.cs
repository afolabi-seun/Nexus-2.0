using System.Net;

namespace BillingService.Domain.Exceptions;

public class PlanNotFoundException : DomainException
{
    public PlanNotFoundException()
        : base(ErrorCodes.PlanNotFoundValue, ErrorCodes.PlanNotFound,
            "Specified plan does not exist or is inactive.", HttpStatusCode.NotFound) { }
}
