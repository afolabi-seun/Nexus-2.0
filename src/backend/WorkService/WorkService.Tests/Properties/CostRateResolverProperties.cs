using FsCheck;
using FsCheck.Xunit;
using WorkService.Domain.Entities;
using WorkService.Infrastructure.Services.CostRates;
using WorkService.Tests.Generators;

namespace WorkService.Tests.Properties;

/// <summary>
/// Feature: time-tracking-cost, Property 15: Cost rate resolution follows precedence hierarchy
/// **Validates: Requirements 7.1, 7.2, 7.4**
/// </summary>
public class CostRateResolverProperties
{
    private readonly CostRateResolver _sut = new();
    private static readonly Guid MemberId = Guid.NewGuid();
    private static readonly Guid DeptId = Guid.NewGuid();
    private static readonly DateTime EntryDate = new(2024, 6, 15);

    // Property 15a: Member rate always wins over role+dept and org default when all exist
    [Property(MaxTest = 100)]
    public bool MemberRate_AlwaysWins_WhenAllLevelsExist(PositiveInt memberRate, PositiveInt roleDeptRate, PositiveInt orgRate)
    {
        var mRate = (decimal)memberRate.Get;
        var rdRate = (decimal)roleDeptRate.Get;
        var oRate = (decimal)orgRate.Get;

        var memberRates = new[] { CostRateGenerators.CreateMemberRate(mRate, EntryDate.AddDays(-10)) };
        var roleDeptRates = new[] { CostRateGenerators.CreateRoleDeptRate(rdRate, EntryDate.AddDays(-10)) };
        var orgDefault = CostRateGenerators.CreateOrgDefault(oRate, EntryDate.AddDays(-10));

        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, memberRates, roleDeptRates, orgDefault);
        return result == mRate;
    }

    // Property 15b: Role+dept rate wins over org default when no member rate exists
    [Property(MaxTest = 100)]
    public bool RoleDeptRate_WinsOverOrgDefault_WhenNoMemberRate(PositiveInt roleDeptRate, PositiveInt orgRate)
    {
        var rdRate = (decimal)roleDeptRate.Get;
        var oRate = (decimal)orgRate.Get;

        var roleDeptRates = new[] { CostRateGenerators.CreateRoleDeptRate(rdRate, EntryDate.AddDays(-5)) };
        var orgDefault = CostRateGenerators.CreateOrgDefault(oRate, EntryDate.AddDays(-5));

        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, Array.Empty<CostRate>(), roleDeptRates, orgDefault);
        return result == rdRate;
    }

    // Property 15c: Org default is used when no member or role+dept rates exist
    [Property(MaxTest = 100)]
    public bool OrgDefault_UsedWhenNoOtherRates(PositiveInt orgRate)
    {
        var oRate = (decimal)orgRate.Get;
        var orgDefault = CostRateGenerators.CreateOrgDefault(oRate, EntryDate.AddDays(-5));

        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, Array.Empty<CostRate>(), Array.Empty<CostRate>(), orgDefault);
        return result == oRate;
    }

    // Property 15d: Returns 0 when no rates exist at any level
    [Property(MaxTest = 100)]
    public bool ReturnsZero_WhenNoRatesExist()
    {
        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, Array.Empty<CostRate>(), Array.Empty<CostRate>(), null);
        return result == 0m;
    }

    // Property 15e: Most recent effectiveFrom <= entryDate is selected within each level
    [Property(MaxTest = 100)]
    public bool MostRecentEffectiveFrom_IsSelected(PositiveInt olderRate, PositiveInt newerRate)
    {
        var older = (decimal)olderRate.Get;
        var newer = (decimal)newerRate.Get;

        var memberRates = new[]
        {
            CostRateGenerators.CreateMemberRate(older, EntryDate.AddDays(-30)),
            CostRateGenerators.CreateMemberRate(newer, EntryDate.AddDays(-1))
        };

        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, memberRates, Array.Empty<CostRate>(), null);
        return result == newer;
    }

    // Property 15f: Same inputs always produce same output (deterministic)
    [Property(MaxTest = 100)]
    public bool Deterministic_SameInputsSameOutput(PositiveInt rate)
    {
        var r = (decimal)rate.Get;
        var memberRates = new[] { CostRateGenerators.CreateMemberRate(r, EntryDate.AddDays(-10)) };

        var result1 = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, memberRates, Array.Empty<CostRate>(), null);
        var result2 = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, memberRates, Array.Empty<CostRate>(), null);
        return result1 == result2;
    }

    // Property 15g: Rates with effectiveFrom > entryDate are ignored
    [Property(MaxTest = 100)]
    public bool FutureRates_AreIgnored(PositiveInt futureRate, PositiveInt pastRate)
    {
        var fRate = (decimal)futureRate.Get;
        var pRate = (decimal)pastRate.Get;

        var memberRates = new[]
        {
            CostRateGenerators.CreateMemberRate(fRate, EntryDate.AddDays(10)),  // future — should be ignored
            CostRateGenerators.CreateMemberRate(pRate, EntryDate.AddDays(-10))  // past — should be selected
        };

        var result = _sut.Resolve(MemberId, "Developer", DeptId, EntryDate, memberRates, Array.Empty<CostRate>(), null);
        return result == pRate;
    }

    // Property 15h: With random CostRate lists, precedence hierarchy is always respected
    [Property(MaxTest = 100)]
    public bool PrecedenceHierarchy_WithRandomRates(PositiveInt seed)
    {
        var rng = new Random(seed.Get);
        var entryDate = new DateTime(2024, 6, 15);

        // Generate random member rates (0-3 rates with varying dates)
        var memberRateCount = rng.Next(0, 4);
        var memberRates = Enumerable.Range(0, memberRateCount)
            .Select(_ => CostRateGenerators.CreateMemberRate(
                (decimal)(rng.Next(10, 500)),
                entryDate.AddDays(-rng.Next(0, 60))))
            .ToArray();

        // Generate random role+dept rates (0-3 rates)
        var roleDeptRateCount = rng.Next(0, 4);
        var roleDeptRates = Enumerable.Range(0, roleDeptRateCount)
            .Select(_ => CostRateGenerators.CreateRoleDeptRate(
                (decimal)(rng.Next(10, 500)),
                entryDate.AddDays(-rng.Next(0, 60))))
            .ToArray();

        // Maybe an org default
        CostRate? orgDefault = rng.Next(2) == 0
            ? CostRateGenerators.CreateOrgDefault((decimal)(rng.Next(10, 500)), entryDate.AddDays(-rng.Next(0, 60)))
            : null;

        var result = _sut.Resolve(MemberId, "Developer", DeptId, entryDate, memberRates, roleDeptRates, orgDefault);

        // Verify precedence: if any member rate is applicable, result must come from member rates
        var applicableMember = memberRates
            .Where(r => r.EffectiveFrom <= entryDate)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefault();

        if (applicableMember != null)
            return result == applicableMember.HourlyRate;

        var applicableRoleDept = roleDeptRates
            .Where(r => r.EffectiveFrom <= entryDate)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefault();

        if (applicableRoleDept != null)
            return result == applicableRoleDept.HourlyRate;

        if (orgDefault != null && orgDefault.EffectiveFrom <= entryDate)
            return result == orgDefault.HourlyRate;

        return result == 0m;
    }
}
