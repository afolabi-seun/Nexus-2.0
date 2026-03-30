using BillingService.Domain.Entities;

namespace BillingService.Domain.Interfaces.Repositories;

public interface IUsageRecordRepository
{
    Task<List<UsageRecord>> GetByOrganizationAndPeriodAsync(Guid organizationId, DateTime periodStart, CancellationToken ct);
    Task UpsertAsync(UsageRecord record, CancellationToken ct);
    Task ArchivePeriodAsync(Guid organizationId, DateTime periodEnd, CancellationToken ct);
}
