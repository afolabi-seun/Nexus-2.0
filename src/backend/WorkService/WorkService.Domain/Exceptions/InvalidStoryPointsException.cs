namespace WorkService.Domain.Exceptions;

public class InvalidStoryPointsException : DomainException
{
    public InvalidStoryPointsException(int points)
        : base(ErrorCodes.InvalidStoryPointsValue, ErrorCodes.InvalidStoryPoints,
            $"Story points value '{points}' is not a valid Fibonacci number (1, 2, 3, 5, 8, 13, 21).") { }
}
