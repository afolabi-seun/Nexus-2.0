using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityService.Api.Attributes;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.AuditLogs;
using UtilityService.Domain.Exceptions;
using UtilityService.Domain.Interfaces.Services;

namespace UtilityService.Api.Controllers;

/// <summary>
/// Manages immutable audit logs — create (service-to-service), query, and archive queries.
/// Audit logs cannot be updated or deleted.
/// </summary>
[ApiController]
[Route("api/v1/audit-logs")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService) => _auditLogService = auditLogService;

    /// <summary>
    /// Create an audit log entry (service-to-service only).
    /// </summary>
    /// <param name="request">Audit log data including organization, service, action, and entity details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created audit log entry</returns>
    /// <response code="201">Audit log created</response>
    /// <response code="403">Service not authorized</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/audit-logs
    ///     {
    ///         "organizationId": "guid",
    ///         "serviceName": "SecurityService",
    ///         "action": "Login",
    ///         "entityType": "User",
    ///         "entityId": "guid",
    ///         "userId": "guid",
    ///         "correlationId": "guid"
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [ServiceAuth]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateAuditLogRequest request, CancellationToken ct)
    {
        var result = await _auditLogService.CreateAsync(request, ct);
        return StatusCode(201, Wrap(result, "Audit log created."));
    }

    /// <summary>
    /// Query audit logs for the current organization.
    /// </summary>
    /// <param name="filter">Filter criteria (serviceName, action, entityType, userId, dateFrom, dateTo)</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated audit logs</returns>
    /// <response code="200">Audit logs retrieved</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Query(
        [FromQuery] AuditLogFilterRequest filter,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _auditLogService.QueryAsync(orgId, filter, page, pageSize, ct);
        return Ok(Wrap(result, "Audit logs retrieved."));
    }

    /// <summary>
    /// Query archived audit logs for the current organization.
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated archived audit logs</returns>
    /// <response code="200">Archived audit logs retrieved</response>
    [HttpGet("archive")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> QueryArchive(
        [FromQuery] AuditLogFilterRequest filter,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _auditLogService.QueryArchiveAsync(orgId, filter, page, pageSize, ct);
        return Ok(Wrap(result, "Archived audit logs retrieved."));
    }

    /// <summary>
    /// Update audit log — not allowed. Audit logs are immutable.
    /// </summary>
    /// <response code="405">Audit logs are immutable and cannot be updated</response>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status405MethodNotAllowed)]
    public ActionResult<ApiResponse<object>> Update()
    {
        throw new AuditLogImmutableException();
    }

    /// <summary>
    /// Delete audit log — not allowed. Audit logs are immutable.
    /// </summary>
    /// <response code="405">Audit logs are immutable and cannot be deleted</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status405MethodNotAllowed)]
    public ActionResult<ApiResponse<object>> Delete()
    {
        throw new AuditLogImmutableException();
    }

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        return new ApiResponse<object>
        {
            Success = true, Data = data, Message = message,
            CorrelationId = HttpContext.Items["CorrelationId"]?.ToString()
        };
    }
}
