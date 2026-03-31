using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.CostRates;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.CostRates;
using WorkService.Domain.Interfaces.Services.CostRates;

namespace WorkService.Infrastructure.Services.CostRates;

public class CostRateService : ICostRateService
{
    private readonly ICostRateRepository _costRateRepo;
    private readonly ILogger<CostRateService> _logger;

    public CostRateService(ICostRateRepository costRateRepo, ILogger<CostRateService> logger)
    {
        _costRateRepo = costRateRepo;
        _logger = logger;
    }

    public async Task<object> CreateAsync(Guid orgId, Guid userId, string userRole, object request, CancellationToken ct = default)
    {
        if (userRole != "OrgAdmin")
            throw new InsufficientPermissionsException();

        var req = (CreateCostRateRequest)request;

        if (req.HourlyRate <= 0)
            throw new InvalidCostRateException("Hourly rate must be greater than zero.");

        var isDuplicate = await _costRateRepo.ExistsDuplicateAsync(
            orgId, req.RateType, req.MemberId, req.RoleName, req.DepartmentId, ct);

        if (isDuplicate)
            throw new CostRateDuplicateException(req.RateType);

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

        return MapToResponse(rate);
    }

    public async Task<object> UpdateAsync(Guid costRateId, Guid userId, string userRole, object request, CancellationToken ct = default)
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

        return MapToResponse(rate);
    }

    public async System.Threading.Tasks.Task DeleteAsync(Guid costRateId, Guid userId, string userRole, CancellationToken ct = default)
    {
        if (userRole != "OrgAdmin")
            throw new InsufficientPermissionsException();

        var rate = await _costRateRepo.GetByIdAsync(costRateId, ct)
            ?? throw new InvalidCostRateException($"Cost rate with ID '{costRateId}' was not found.");

        rate.FlgStatus = "D";
        rate.DateUpdated = DateTime.UtcNow;

        await _costRateRepo.UpdateAsync(rate, ct);
    }

    public async Task<object> ListAsync(Guid orgId, string? rateType, Guid? memberId,
        Guid? departmentId, string? roleName, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await _costRateRepo.ListAsync(
            orgId, rateType, memberId, departmentId, roleName, page, pageSize, ct);

        var responses = items.Select(MapToResponse).ToList();

        return new PaginatedResponse<CostRateResponse>
        {
            Data = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
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
