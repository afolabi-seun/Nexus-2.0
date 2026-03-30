using System.Net;

namespace WorkService.Domain.Exceptions;

public class LabelNameDuplicateException : DomainException
{
    public LabelNameDuplicateException(string name)
        : base(ErrorCodes.LabelNameDuplicateValue, ErrorCodes.LabelNameDuplicate,
            $"A label with name '{name}' already exists.", HttpStatusCode.Conflict) { }
}
