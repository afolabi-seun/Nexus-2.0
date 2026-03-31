using FsCheck;
using FsCheck.Xunit;
using WorkService.Domain.Entities;
using WorkService.Infrastructure.Services.CostRates;

namespace WorkService.Tests.Properties;

public static class CostRateGenerator
{
    public static CostRate Create(decimal hourlyRate, DateTime effectiveFrom) => new()
    {
        CostRateId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        HourlyRate = hourlyRate,
        EffectiveFrom = effectiveFrom,
        RateType = "Member",
        FlgStatus = "A"
    };
}

public class CostRateResolverProperties
{
    private readonly CostRateResolver _sut = new();
    private static readonly Guid MemberId = Guid.NewGuid();
    private static readonly Guid DeptId = Guid.NewGuid();
    private static readonly DateTime EntryDate = new(2024, 6, 15);

    // Feature: time-tracking-cost, Property 1: Member rate always wins over role+dept and org default when all exist
    [Property(MaxTest = 100)]
    public bool MemberRate_AlwaysWins_WhenAllLevelsExist(PositiveInt memberRate, PositiveInt roleDeptRate, PositiveInt orgRate)
    {
        var mRate = (decimal)memberRate.Get;
        var rdRate = (decimal)roleDeptRate.Get;
        var oRate = (decimal)orgRate.Get;

        var memberRates = new[] { CostRateGenerator.Create(mRate, EntryDate.AddDays(-10)) };
        var roleDeptRates = new[] { CostRateGenerator.Create(rdRate, EntryDate.AddDays(-10)) };
        var orgDefault = CostRateGenerator.Create(oRate, EntryDate.AddDays(-10));

        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, memberRates, roleDeptRates, orgDefault);
        return result == mRate;
    }

    // Feature: time-tracking-cost, Property 2: Role+dept rate wins over org default when no member rate exists
    [Property(MaxTest = 100)]
    public bool RoleDeptRate_WinsOverOrgDefault_WhenNoMemberRate(PositiveInt roleDeptRate, PositiveInt orgRate)
    {
        var rdRate = (decimal)roleDeptRate.Get;
        var oRate = (decimal)orgRate.Get;

        var roleDeptRates = new[] { CostRateGenerator.Create(rdRate, EntryDate.AddDays(-5)) };
        var orgDefault = CostRateGenerator.Create(oRate, EntryDate.AddDays(-5));

        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, Array.Empty<CostRate>(), roleDeptRates, orgDefault);
        return result == rdRate;
    }

    // Feature: time-tracking-cost, Property 3: Org default is used when no member or role+dept rates exist
    [Property(MaxTest = 100)]
    public bool OrgDefault_UsedWhenNoOtherRates(PositiveInt orgRate)
    {
        var oRate = (decimal)orgRate.Get;
        var orgDefault = CostRateGenerator.Create(oRate, EntryDate.AddDays(-5));

        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, Array.Empty<CostRate>(), Array.Empty<CostRate>(), orgDefault);
        return result == oRate;
    }

    // Feature: time-tracking-cost, Property 4: Returns 0 when no rates exist at any level
    [Property(MaxTest = 100)]
    public bool ReturnsZero_WhenNoRatesExist()
    {
        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, Array.Empty<CostRate>(), Array.Empty<CostRate>(), null);
        return result == 0m;
    }

    // Feature: time-tracking-cost, Property 5: Most recent effectiveFrom <= entryDate is selected within each level
    [Property(MaxTest = 100)]
    public bool MostRecentEffectiveFrom_IsSelected(PositiveInt olderRate, PositiveInt newerRate)
    {
        var older = (decimal)olderRate.Get;
        var newer = (decimal)newerRate.Get;

        var memberRates = new[]
        {
            CostRateGenerator.Create(older, EntryDate.AddDays(-30)),
            CostRateGenerator.Create(newer, EntryDate.AddDays(-1))
        };

        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, memberRates, Array.Empty<CostRate>(), null);
        return result == newer;
    }

    // Feature: time-tracking-cost, Property 6: Same inputs always produce same output (deterministic)
    [Property(MaxTest = 100)]
    public bool Deterministic_SameInputsSameOutput(PositiveInt rate)
    {
        var r = (decimal)rate.Get;
        var memberRates = new[] { CostRateGenerator.Create(r, EntryDate.AddDays(-10)) };

        var result1 = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, memberRates, Array.Empty<CostRate>(), null);
        var result2 = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, memberRates, Array.Empty<CostRate>(), null);
        return result1 == result2;
    }

    // Feature: time-tracking-cost, Property 7: Rates with effectiveFrom > entryDate are ignored
    [Property(MaxTest = 100)]
    public bool FutureRates_AreIgnored(PositiveInt futureRate, PositiveInt pastRate)
    {
        var fRate = (decimal)futureRate.Get;
        var pRate = (decimal)pastRate.Get;

        var memberRates = new[]
        {
            CostRateGenerator.Create(fRate, EntryDate.AddDays(10)),  // future — should be ignored
            CostRateGenerator.Create(pRate, EntryDate.AddDays(-10))  // past — should be selected
        };

        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, memberRates, Array.Empty<CostRate>(), null);
        return result == pRate;
    }
}
