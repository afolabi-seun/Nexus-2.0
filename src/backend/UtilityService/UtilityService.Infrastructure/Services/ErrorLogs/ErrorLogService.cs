using UtilityService.Domain.Results;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.ErrorLogs;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.ErrorLogs;
using UtilityService.Domain.Interfaces.Services.ErrorLogs;
using UtilityService.Domain.Interfaces.Services.PiiRedaction;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Services.ErrorLogs;

public class ErrorLogService : IErrorLogService
{
    private readonly IErrorLogRepository _repo;
    private readonly IPiiRedactionService _piiRedaction;
    private readonly UtilityDbContext _dbContext;

    public ErrorLogService(IErrorLogRepository repo, IPiiRedactionService piiRedaction, UtilityDbContext dbContext)
    {
        _repo = repo;
        _piiRedaction = piiRedaction;
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<object>> CreateAsync(object request, CancellationToken ct = default)
    {
        var req = (CreateErrorLogRequest)request;
        var entity = new ErrorLog
        {
            OrganizationId = req.OrganizationId,
            ServiceName = req.ServiceName,
            ErrorCode = req.ErrorCode,
            Message = _piiRedaction.Redact(req.Message),
            StackTrace = req.StackTrace != null ? _piiRedaction.Redact(req.StackTrace) : null,
            CorrelationId = req.CorrelationId,
            Severity = req.Severity,
            DateCreated = DateTime.UtcNow
        };

        var created = await _repo.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);
        return ServiceResult<object>.Created(MapToResponse(created), "Error log created.");
    }

    public async Task<ServiceResult<object>> QueryAsync(Guid organizationId, object filter, int page, int pageSize, CancellationToken ct = default)
    {
        var f = (ErrorLogFilterRequest)filter;
        var (items, totalCount) = await _repo.QueryAsync(
            organizationId, f.ServiceName, f.ErrorCode, f.Severity, f.DateFrom, f.DateTo, page, pageSize, ct);

        var result = new PaginatedResponse<ErrorLogResponse>
        {
            TotalCount = totalCount, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Data = items.Select(MapToResponse)
        };
        return ServiceResult<object>.Ok(result, "Error logs retrieved.");
    }

    private static ErrorLogResponse MapToResponse(ErrorLog e) => new()
    {
        ErrorLogId = e.ErrorLogId,
        OrganizationId = e.OrganizationId,
        ServiceName = e.ServiceName,
        ErrorCode = e.ErrorCode,
        Message = e.Message,
        StackTrace = e.StackTrace,
        CorrelationId = e.CorrelationId,
        Severity = e.Severity,
        DateCreated = e.DateCreated
    };
}
