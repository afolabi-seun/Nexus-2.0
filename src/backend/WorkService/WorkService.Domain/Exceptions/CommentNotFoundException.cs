using System.Net;

namespace WorkService.Domain.Exceptions;

public class CommentNotFoundException : DomainException
{
    public CommentNotFoundException(Guid commentId)
        : base(ErrorCodes.CommentNotFoundValue, ErrorCodes.CommentNotFound,
            $"Comment with ID '{commentId}' was not found.", HttpStatusCode.NotFound) { }
}
