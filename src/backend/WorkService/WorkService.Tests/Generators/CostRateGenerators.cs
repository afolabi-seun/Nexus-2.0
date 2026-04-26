using WorkService.Domain.Entities;

namespace WorkService.Tests.Generators;

public static class CostRateGenerators
{
    public static CostRate CreateMemberRate(decimal hourlyRate, DateTime effectiveFrom) => new()
    {
        CostRateId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        HourlyRate = hourlyRate,
        EffectiveFrom = effectiveFrom,
        RateType = "Member",
        MemberId = Guid.NewGuid(),
        FlgStatus = "A"
    };

    public static CostRate CreateRoleDeptRate(decimal hourlyRate, DateTime effectiveFrom) => new()
    {
        CostRateId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        HourlyRate = hourlyRate,
        EffectiveFrom = effectiveFrom,
        RateType = "RoleDepartment",
        RoleName = "Developer",
        DepartmentId = Guid.NewGuid(),
        FlgStatus = "A"
    };

    public static CostRate CreateOrgDefault(decimal hourlyRate, DateTime effectiveFrom) => new()
    {
        CostRateId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        HourlyRate = hourlyRate,
        EffectiveFrom = effectiveFrom,
        RateType = "OrgDefault",
        FlgStatus = "A"
    };
}
