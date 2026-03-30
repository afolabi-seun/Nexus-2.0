namespace WorkService.Domain.Exceptions;

public class MentionUserNotFoundException : DomainException
{
    public MentionUserNotFoundException(string mention)
        : base(ErrorCodes.MentionUserNotFoundValue, ErrorCodes.MentionUserNotFound,
            $"Mentioned user '{mention}' was not found.") { }
}
