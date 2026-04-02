using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.AuditLogs;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Exceptions;
using UtilityService.Domain.Interfaces.Repositories.ArchivedAuditLogs;
using UtilityService.Domain.Interfaces.Repositories.AuditLogs;
using UtilityService.Domain.Interfaces.Services.AuditLogs;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Services.AuditLogs;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepo;
    private readonly IArchivedAuditLogRepository _archivedRepo;
    private readonly UtilityDbContext _dbContext;

    public AuditLogService(IAuditLogRepository auditLogRepo, IArchivedAuditLogRepository archivedRepo, UtilityDbContext dbContext)
    {
        _auditLogRepo = auditLogRepo;
        _archivedRepo = archivedRepo;
        _dbContext = dbContext;
    }

    public async Task<object> CreateAsync(object request, CancellationToken ct = default)
    {
        var req = (CreateAuditLogRequest)request;
        var entity = new AuditLog
        {
            OrganizationId = req.OrganizationId,
            ServiceName = req.ServiceName,
            Action = req.Action,
            EntityType = req.EntityType,
            EntityId = req.EntityId,
            UserId = req.UserId,
            OldValue = req.OldValue,
            NewValue = req.NewValue,
            IpAddress = req.IpAddress,
            CorrelationId = req.CorrelationId,
            DateCreated = DateTime.UtcNow
        };

        var created = await _auditLogRepo.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);
        return MapToResponse(created);
    }

    public async Task<object> QueryAsync(Guid organizationId, object filter, int page, int pageSize, CancellationToken ct = default)
    {
        var f = (AuditLogFilterRequest)filter;
        var (items, totalCount) = await _auditLogRepo.QueryAsync(
            organizationId, f.ServiceName, f.Action, f.EntityType, f.UserId, f.DateFrom, f.DateTo, page, pageSize, ct);

        return new PaginatedResponse<AuditLogResponse>
        {
            TotalCount = totalCount, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Data = items.Select(MapToResponse)
        };
    }

    public async Task<object> QueryArchiveAsync(Guid organizationId, object filter, int page, int pageSize, CancellationToken ct = default)
    {
        var f = (AuditLogFilterRequest)filter;
        var (items, totalCount) = await _archivedRepo.QueryAsync(
            organizationId, f.ServiceName, f.Action, f.EntityType, f.UserId, f.DateFrom, f.DateTo, page, pageSize, ct);

        return new PaginatedResponse<AuditLogResponse>
        {
            TotalCount = totalCount, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Data = items.Select(a => new AuditLogResponse
            {
                AuditLogId = a.ArchivedAuditLogId,
                OrganizationId = a.OrganizationId,
                ServiceName = a.ServiceName,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                UserId = a.UserId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                IpAddress = a.IpAddress,
                CorrelationId = a.CorrelationId,
                DateCreated = a.DateCreated,
                ArchivedDate = a.ArchivedDate
            })
        };
    }

    private static AuditLogResponse MapToResponse(AuditLog e) => new()
    {
        AuditLogId = e.AuditLogId,
        OrganizationId = e.OrganizationId,
        ServiceName = e.ServiceName,
        Action = e.Action,
        EntityType = e.EntityType,
        EntityId = e.EntityId,
        UserId = e.UserId,
        OldValue = e.OldValue,
        NewValue = e.NewValue,
        IpAddress = e.IpAddress,
        CorrelationId = e.CorrelationId,
        DateCreated = e.DateCreated
    };
}
