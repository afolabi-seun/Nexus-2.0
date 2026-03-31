using System.Net;

namespace WorkService.Domain.Exceptions;

public class CostRateDuplicateException : DomainException
{
    public CostRateDuplicateException(string rateType)
        : base(ErrorCodes.CostRateDuplicateValue, ErrorCodes.CostRateDuplicate,
            $"A cost rate of type '{rateType}' already exists for the specified scope.", HttpStatusCode.Conflict) { }
}
