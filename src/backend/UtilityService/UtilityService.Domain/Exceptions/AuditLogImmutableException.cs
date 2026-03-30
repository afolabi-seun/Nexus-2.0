using System.Net;

namespace UtilityService.Domain.Exceptions;

public class AuditLogImmutableException : DomainException
{
    public AuditLogImmutableException(string message = "Audit logs are immutable and cannot be modified or deleted.")
        : base(ErrorCodes.AuditLogImmutableValue, ErrorCodes.AuditLogImmutable, message, HttpStatusCode.MethodNotAllowed) { }
}
