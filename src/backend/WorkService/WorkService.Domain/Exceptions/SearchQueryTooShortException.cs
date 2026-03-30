namespace WorkService.Domain.Exceptions;

public class SearchQueryTooShortException : DomainException
{
    public SearchQueryTooShortException()
        : base(ErrorCodes.SearchQueryTooShortValue, ErrorCodes.SearchQueryTooShort,
            "Search query must be at least 2 characters long.") { }
}
