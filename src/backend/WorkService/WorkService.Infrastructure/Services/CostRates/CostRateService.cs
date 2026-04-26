using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.CostRates;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.CostRates;
using WorkService.Domain.Interfaces.Services.CostRates;
using WorkService.Domain.Results;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Services.CostRates;

public class CostRateService : ICostRateService
{
    private readonly ICostRateRepository _costRateRepo;
    private readonly WorkDbContext _dbContext;
    private readonly ILogger<CostRateService> _logger;

    public CostRateService(ICostRateRepository costRateRepo, WorkDbContext dbContext, ILogger<CostRateService> logger)
    {
        _costRateRepo = costRateRepo;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ServiceResult<object>> CreateAsync(Guid orgId, Guid userId, string userRole, object request, CancellationToken ct = default)
    {
        if (userRole != "OrgAdmin")
            throw new InsufficientPermissionsException();

        var req = (CreateCostRateRequest)request;

        if (req.HourlyRate <= 0)
            throw new InvalidCostRateException("Hourly rate must be greater than zero.");

        var isDuplicate = await _costRateRepo.ExistsDuplicateAsync(
            orgId, req.RateType, req.MemberId, req.RoleName, req.DepartmentId, ct);

        if (isDuplicate)
            return ServiceResult<object>.Fail(4053, "COST_RATE_DUPLICATE", $"A cost rate for type '{req.RateType}' already exists.", 409);

        var rate = new CostRate
        {
            OrganizationId = orgId,
            RateType = req.RateType,
            MemberId = req.MemberId,
            RoleName = req.RoleName,
            DepartmentId = req.DepartmentId,
            HourlyRate = req.HourlyRate,
            EffectiveFrom = req.EffectiveFrom ?? DateTime.UtcNow
        };

        await _costRateRepo.AddAsync(rate, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.Created(MapToResponse(rate), "Cost rate created successfully.");
    }

    public async Task<ServiceResult<object>> UpdateAsync(Guid costRateId, Guid userId, string userRole, object request, CancellationToken ct = default)
    {
        if (userRole != "OrgAdmin")
            throw new InsufficientPermissionsException();

        var req = (UpdateCostRateRequest)request;

        if (req.HourlyRate <= 0)
            throw new InvalidCostRateException("Hourly rate must be greater than zero.");

        var rate = await _costRateRepo.GetByIdAsync(costRateId, ct)
            ?? throw new InvalidCostRateException($"Cost rate with ID '{costRateId}' was not found.");

        rate.HourlyRate = req.HourlyRate;
        if (req.EffectiveFrom.HasValue)
            rate.EffectiveFrom = req.EffectiveFrom.Value;
        rate.DateUpdated = DateTime.UtcNow;

        await _costRateRepo.UpdateAsync(rate, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.Ok(MapToResponse(rate), "Cost rate updated.");
    }

    public async Task<ServiceResult<object>> DeleteAsync(Guid costRateId, Guid userId, string userRole, CancellationToken ct = default)
    {
        if (userRole != "OrgAdmin")
            throw new InsufficientPermissionsException();

        var rate = await _costRateRepo.GetByIdAsync(costRateId, ct)
            ?? throw new InvalidCostRateException($"Cost rate with ID '{costRateId}' was not found.");

        rate.FlgStatus = "D";
        rate.DateUpdated = DateTime.UtcNow;

        await _costRateRepo.UpdateAsync(rate, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.NoContent("Cost rate deleted.");
    }

    public async Task<ServiceResult<object>> ListAsync(Guid orgId, string? rateType, Guid? memberId,
        Guid? departmentId, string? roleName, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await _costRateRepo.ListAsync(
            orgId, rateType, memberId, departmentId, roleName, page, pageSize, ct);

        var responses = items.Select(MapToResponse).ToList();

        return ServiceResult<object>.Ok(new PaginatedResponse<CostRateResponse>
        {
            Data = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        }, "Cost rates retrieved.");
    }

    private static CostRateResponse MapToResponse(CostRate rate)
    {
        return new CostRateResponse
        {
            CostRateId = rate.CostRateId,
            OrganizationId = rate.OrganizationId,
            RateType = rate.RateType,
            MemberId = rate.MemberId,
            RoleName = rate.RoleName,
            DepartmentId = rate.DepartmentId,
            HourlyRate = rate.HourlyRate,
            EffectiveFrom = rate.EffectiveFrom,
            FlgStatus = rate.FlgStatus,
            DateCreated = rate.DateCreated,
            DateUpdated = rate.DateUpdated
        };
    }
}
