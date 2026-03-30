namespace WorkService.Domain.Exceptions;

public class InvalidPriorityException : DomainException
{
    public InvalidPriorityException(string priority)
        : base(ErrorCodes.InvalidPriorityValue, ErrorCodes.InvalidPriority,
            $"Priority '{priority}' is not valid. Must be Critical, High, Medium, or Low.") { }
}
