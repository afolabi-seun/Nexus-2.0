using WorkService.Domain.Entities;

namespace WorkService.Domain.Interfaces.Services.CostRates;

public interface ICostRateResolver
{
    /// <summary>
    /// Resolves the applicable hourly rate for a time entry.
    /// Precedence: Member rate → Role+Department rate → Org default.
    /// Within each level, picks the most recent effectiveFrom &lt;= entryDate.
    /// Returns 0 if no rate found.
    /// </summary>
    decimal Resolve(
        Guid memberId, string roleName, Guid departmentId, DateTime entryDate,
        IEnumerable<CostRate> memberRates,
        IEnumerable<CostRate> roleDeptRates,
        CostRate? orgDefault);
}
