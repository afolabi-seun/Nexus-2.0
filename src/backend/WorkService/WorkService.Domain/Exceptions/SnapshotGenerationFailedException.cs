using System.Net;

namespace WorkService.Domain.Exceptions;

public class SnapshotGenerationFailedException : DomainException
{
    public SnapshotGenerationFailedException(string message)
        : base(ErrorCodes.SnapshotGenerationFailedValue, ErrorCodes.SnapshotGenerationFailed,
            message, HttpStatusCode.InternalServerError) { }
}
