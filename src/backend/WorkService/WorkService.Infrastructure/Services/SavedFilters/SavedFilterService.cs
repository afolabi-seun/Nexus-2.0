using WorkService.Application.DTOs.SavedFilters;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.SavedFilters;
using WorkService.Domain.Interfaces.Services.SavedFilters;
using WorkService.Domain.Results;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Services.SavedFilters;

public class SavedFilterService : ISavedFilterService
{
    private readonly ISavedFilterRepository _savedFilterRepo;
    private readonly WorkDbContext _dbContext;

    public SavedFilterService(ISavedFilterRepository savedFilterRepo, WorkDbContext dbContext)
    {
        _savedFilterRepo = savedFilterRepo;
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<object>> CreateAsync(Guid orgId, Guid userId, object request, CancellationToken ct = default)
    {
        var req = (CreateSavedFilterRequest)request;

        var filter = new SavedFilter
        {
            SavedFilterId = Guid.NewGuid(),
            OrganizationId = orgId,
            TeamMemberId = userId,
            Name = req.Name,
            Filters = req.Filters,
            DateCreated = DateTime.UtcNow
        };

        var result = await _savedFilterRepo.AddAsync(filter, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.Created(new SavedFilterResponse
        {
            SavedFilterId = result.SavedFilterId,
            Name = result.Name,
            Filters = result.Filters,
            DateCreated = result.DateCreated
        }, "Saved filter created.");
    }

    public async Task<ServiceResult<object>> ListAsync(Guid orgId, Guid userId, CancellationToken ct = default)
    {
        var filters = await _savedFilterRepo.ListByMemberAsync(orgId, userId, ct);
        var result = filters.Select(f => new SavedFilterResponse
        {
            SavedFilterId = f.SavedFilterId,
            Name = f.Name,
            Filters = f.Filters,
            DateCreated = f.DateCreated
        });

        return ServiceResult<object>.Ok(result, "Saved filters retrieved.");
    }

    public async Task<ServiceResult<object>> DeleteAsync(Guid filterId, CancellationToken ct = default)
    {
        var filter = await _savedFilterRepo.GetByIdAsync(filterId, ct);
        if (filter == null)
            return ServiceResult<object>.Fail(4070, "FILTER_NOT_FOUND", "Saved filter not found.", 404);

        await _savedFilterRepo.DeleteAsync(filter, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.NoContent("Saved filter deleted.");
    }
}
