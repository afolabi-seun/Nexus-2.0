using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Services.CostRates;

namespace WorkService.Infrastructure.Services.CostRates;

public class CostRateResolver : ICostRateResolver
{
    /// <inheritdoc />
    public decimal Resolve(
        Guid memberId, string roleName, Guid departmentId, DateTime entryDate,
        IEnumerable<CostRate> memberRates,
        IEnumerable<CostRate> roleDeptRates,
        CostRate? orgDefault)
    {
        // 1. Member-specific rate: most recent EffectiveFrom <= entryDate
        var memberRate = memberRates
            .Where(r => r.EffectiveFrom <= entryDate)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefault();

        if (memberRate is not null)
            return memberRate.HourlyRate;

        // 2. Role + Department rate: most recent EffectiveFrom <= entryDate
        var roleDeptRate = roleDeptRates
            .Where(r => r.EffectiveFrom <= entryDate)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefault();

        if (roleDeptRate is not null)
            return roleDeptRate.HourlyRate;

        // 3. Org default rate
        if (orgDefault is not null && orgDefault.EffectiveFrom <= entryDate)
            return orgDefault.HourlyRate;

        // 4. No rate found
        return 0;
    }
}
