using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories.UsageRecords;
using BillingService.Infrastructure.Data;
using BillingService.Infrastructure.Repositories.Generics;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Infrastructure.Repositories.UsageRecords;

public class UsageRecordRepository : GenericRepository<UsageRecord>, IUsageRecordRepository
{
    private readonly BillingDbContext _db;

    public UsageRecordRepository(BillingDbContext context) : base(context)
    {
        _db = context;
    }

    public async Task<List<UsageRecord>> GetByOrganizationAndPeriodAsync(Guid organizationId, DateTime periodStart, CancellationToken ct) =>
        await _db.UsageRecords
            .IgnoreQueryFilters()
            .Where(u => u.OrganizationId == organizationId && u.PeriodStart == periodStart)
            .ToListAsync(ct);

    public async Task UpsertAsync(UsageRecord record, CancellationToken ct)
    {
        var existing = await _db.UsageRecords
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u =>
                u.OrganizationId == record.OrganizationId &&
                u.MetricName == record.MetricName &&
                u.PeriodStart == record.PeriodStart, ct);

        if (existing is not null)
        {
            existing.MetricValue = record.MetricValue;
            existing.DateUpdated = DateTime.UtcNow;
            _db.UsageRecords.Update(existing);
        }
        else
        {
            await _db.UsageRecords.AddAsync(record, ct);
        }
    }

    public async Task ArchivePeriodAsync(Guid organizationId, DateTime periodEnd, CancellationToken ct)
    {
        await Task.CompletedTask;
    }

    public async Task<List<UsageRecord>> GetAllCurrentPeriodAsync(DateTime periodStart, CancellationToken ct) =>
        await _db.UsageRecords
            .IgnoreQueryFilters()
            .Where(u => u.PeriodStart >= periodStart)
            .ToListAsync(ct);
}
