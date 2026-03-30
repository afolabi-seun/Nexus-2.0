using System.Net;

namespace WorkService.Domain.Exceptions;

public class CommentNotAuthorException : DomainException
{
    public CommentNotAuthorException()
        : base(ErrorCodes.CommentNotAuthorValue, ErrorCodes.CommentNotAuthor,
            "Only the comment author can edit this comment.", HttpStatusCode.Forbidden) { }
}
