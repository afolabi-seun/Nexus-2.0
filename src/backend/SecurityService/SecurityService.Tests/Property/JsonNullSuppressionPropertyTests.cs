using System.Text.Json;
using System.Text.Json.Serialization;
using FsCheck;
using FsCheck.Xunit;
using SecurityService.Application.DTOs;

namespace SecurityService.Tests.Property;

/// <summary>
/// Property-based tests for JSON null field suppression.
/// Feature: architecture-hardening, Property 1: Null field suppression in serialized JSON
/// **Validates: Requirements 1.2, 1.3**
/// </summary>
public class JsonNullSuppressionPropertyTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// For any ApiResponse&lt;object&gt; with an arbitrary combination of null and non-null fields,
    /// serializing with the configured JsonSerializerOptions produces JSON where every null field
    /// is absent and every non-null field is present.
    /// **Validates: Requirements 1.2, 1.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool NullFields_AreAbsent_NonNullFields_ArePresent(ushort seed)
    {
        var rng = new Random(seed);
        var response = GenerateRandomApiResponse(rng);

        var json = JsonSerializer.Serialize(response, SerializerOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Check nullable fields: each must be absent when null, present when non-null
        if (!CheckField(root, "data", response.Data)) return false;
        if (!CheckField(root, "errorCode", response.ErrorCode)) return false;
        if (!CheckField(root, "errorValue", response.ErrorValue)) return false;
        if (!CheckField(root, "message", response.Message)) return false;
        if (!CheckField(root, "correlationId", response.CorrelationId)) return false;
        if (!CheckField(root, "errors", response.Errors)) return false;

        // Non-nullable fields must always be present
        if (!root.TryGetProperty("responseCode", out _)) return false;
        if (!root.TryGetProperty("responseDescription", out _)) return false;
        if (!root.TryGetProperty("success", out _)) return false;

        return true;
    }

    private static bool CheckField(JsonElement root, string fieldName, object? value)
    {
        var present = root.TryGetProperty(fieldName, out _);
        if (value is null && present) return false;   // null field should be absent
        if (value is not null && !present) return false; // non-null field should be present
        return true;
    }

    private static ApiResponse<object> GenerateRandomApiResponse(Random rng)
    {
        var hasData = rng.Next(2) == 0;
        var hasErrorCode = rng.Next(2) == 0;
        var hasErrorValue = rng.Next(2) == 0;
        var hasMessage = rng.Next(2) == 0;
        var hasCorrelationId = rng.Next(2) == 0;
        var hasErrors = rng.Next(2) == 0;

        return new ApiResponse<object>
        {
            Success = rng.Next(2) == 0,
            ResponseCode = rng.Next(100).ToString("D2"),
            ResponseDescription = $"desc-{rng.Next(10000)}",
            Data = hasData ? new { Name = $"item-{rng.Next(1000)}", Value = rng.Next(999) } : null,
            ErrorCode = hasErrorCode ? $"ERR_{rng.Next(10000)}" : null,
            ErrorValue = hasErrorValue ? rng.Next(1, 10000) : null,
            Message = hasMessage ? $"msg-{rng.Next(10000)}" : null,
            CorrelationId = hasCorrelationId ? Guid.NewGuid().ToString() : null,
            Errors = hasErrors
                ? Enumerable.Range(0, rng.Next(1, 4))
                    .Select(_ => new ErrorDetail
                    {
                        Field = $"field-{rng.Next(100)}",
                        Message = $"error-{rng.Next(100)}"
                    }).ToList()
                : null
        };
    }
}
