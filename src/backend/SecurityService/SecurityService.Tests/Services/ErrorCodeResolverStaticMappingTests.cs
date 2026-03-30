using System.Reflection;
using FsCheck;
using FsCheck.Xunit;
using SecurityService.Domain.Exceptions;
using SecurityService.Infrastructure.Services.ErrorCodeResolver;

namespace SecurityService.Tests.Services;

/// <summary>
/// Property-based tests for ErrorCodeResolverService static mapping.
/// Validates: REQ-031.5, REQ-031.6
/// </summary>
public class ErrorCodeResolverStaticMappingTests
{
    private static readonly string[] KnownErrorCodes = typeof(ErrorCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(string))
        .Select(f => (string)f.GetValue(null)!)
        .ToArray();

    /// <summary>
    /// Property: Every known error code maps to a non-empty response code.
    /// **Validates: Requirements 17.1**
    /// </summary>
    [Property(MaxTest = 200)]
    public bool KnownErrorCode_MapsToNonEmptyResponseCode(ushort seed)
    {
        var errorCode = KnownErrorCodes[seed % KnownErrorCodes.Length];
        var responseCode = ErrorCodeResolverService.MapErrorToResponseCode(errorCode);
        return !string.IsNullOrEmpty(responseCode);
    }

    /// <summary>
    /// Property: Mapping is deterministic — same input always produces same output.
    /// **Validates: Requirements 17.1**
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Mapping_IsDeterministic(ushort seed)
    {
        var errorCode = KnownErrorCodes[seed % KnownErrorCodes.Length];
        var result1 = ErrorCodeResolverService.MapErrorToResponseCode(errorCode);
        var result2 = ErrorCodeResolverService.MapErrorToResponseCode(errorCode);
        return result1 == result2;
    }
}
