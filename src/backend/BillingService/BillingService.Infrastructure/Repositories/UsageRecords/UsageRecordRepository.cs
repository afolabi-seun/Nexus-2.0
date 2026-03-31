using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories;
using BillingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Infrastructure.Repositories.UsageRecords;

public class UsageRecordRepository : IUsageRecordRepository
{
    private readonly BillingDbContext _context;

    public UsageRecordRepository(BillingDbContext context) => _context = context;

    public async Task<List<UsageRecord>> GetByOrganizationAndPeriodAsync(Guid organizationId, DateTime periodStart, CancellationToken ct) =>
        await _context.UsageRecords
            .IgnoreQueryFilters()
            .Where(u => u.OrganizationId == organizationId && u.PeriodStart == periodStart)
            .ToListAsync(ct);

    public async Task UpsertAsync(UsageRecord record, CancellationToken ct)
    {
        var existing = await _context.UsageRecords
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u =>
                u.OrganizationId == record.OrganizationId &&
                u.MetricName == record.MetricName &&
                u.PeriodStart == record.PeriodStart, ct);

        if (existing is not null)
        {
            existing.MetricValue = record.MetricValue;
            existing.DateUpdated = DateTime.UtcNow;
            _context.UsageRecords.Update(existing);
        }
        else
        {
            await _context.UsageRecords.AddAsync(record, ct);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task ArchivePeriodAsync(Guid organizationId, DateTime periodEnd, CancellationToken ct)
    {
        // Archive is a no-op for now — records are kept for historical queries
        await Task.CompletedTask;
    }

    public async Task<List<UsageRecord>> GetAllCurrentPeriodAsync(DateTime periodStart, CancellationToken ct) =>
        await _context.UsageRecords
            .IgnoreQueryFilters()
            .Where(u => u.PeriodStart >= periodStart)
            .ToListAsync(ct);
}
