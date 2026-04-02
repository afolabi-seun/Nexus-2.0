using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories.Generics;

namespace BillingService.Domain.Interfaces.Repositories.UsageRecords;

public interface IUsageRecordRepository : IGenericRepository<UsageRecord>
{
    Task<List<UsageRecord>> GetByOrganizationAndPeriodAsync(Guid organizationId, DateTime periodStart, CancellationToken ct);
    Task UpsertAsync(UsageRecord record, CancellationToken ct);
    Task ArchivePeriodAsync(Guid organizationId, DateTime periodEnd, CancellationToken ct);
    Task<List<UsageRecord>> GetAllCurrentPeriodAsync(DateTime periodStart, CancellationToken ct);
}
