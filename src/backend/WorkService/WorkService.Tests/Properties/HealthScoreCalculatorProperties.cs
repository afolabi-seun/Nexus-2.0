using FsCheck;
using FsCheck.Xunit;
using WorkService.Application.DTOs.Analytics;
using WorkService.Domain.Entities;
using WorkService.Infrastructure.Services.Analytics;

namespace WorkService.Tests.Properties;

public static class HealthScoreGenerator
{
    private static readonly string[] Severities = { "Low", "Medium", "High", "Critical" };

    public static VelocitySnapshot CreateVelocity(int committed, int completed) => new()
    {
        VelocitySnapshotId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        SprintId = Guid.NewGuid(),
        SprintName = "Sprint",
        StartDate = DateTime.UtcNow.AddDays(-14),
        EndDate = DateTime.UtcNow,
        CommittedPoints = committed,
        CompletedPoints = completed,
        CompletedStoryCount = completed,
        SnapshotDate = DateTime.UtcNow
    };

    public static RiskRegister CreateRisk(string severity) => new()
    {
        RiskRegisterId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Title = "Risk",
        Severity = severity,
        Likelihood = "Medium",
        MitigationStatus = "Open",
        CreatedBy = Guid.NewGuid()
    };

    public static string RandomSeverity(int seed) => Severities[Math.Abs(seed) % Severities.Length];
}

public class HealthScoreCalculatorProperties
{
    private readonly HealthScoreCalculator _sut = new();

    // Feature: analytics-reporting, Property 1: Overall score = velocityScore × 0.30 + bugRateScore × 0.25 + overdueScore × 0.25 + riskScore × 0.20
    [Property(MaxTest = 100)]
    public bool OverallScore_MatchesWeightedFormula(ushort seed)
    {
        var rng = new Random(seed);
        var totalStories = rng.Next(1, 200);
        var bugs = rng.Next(0, totalStories + 1);
        var overdue = rng.Next(0, totalStories + 1);
        var riskCount = rng.Next(0, 6);

        var velocities = Enumerable.Range(0, rng.Next(0, 4))
            .Select(_ => HealthScoreGenerator.CreateVelocity(rng.Next(1, 50), rng.Next(0, 50)))
            .ToList();
        var risks = Enumerable.Range(0, riskCount)
            .Select(i => HealthScoreGenerator.CreateRisk(HealthScoreGenerator.RandomSeverity(seed + i)))
            .ToList();

        var result = (HealthScoreResult)_sut.Calculate(velocities, bugs, totalStories, overdue, totalStories, risks);

        var expected = Math.Round(
            result.VelocityScore * 0.30m +
            result.BugRateScore * 0.25m +
            result.OverdueScore * 0.25m +
            result.RiskScore * 0.20m, 2);

        return result.OverallScore == expected;
    }

    // Feature: analytics-reporting, Property 2: Overall score is always in [0, 100]
    [Property(MaxTest = 100)]
    public bool OverallScore_AlwaysInRange(ushort seed)
    {
        var rng = new Random(seed);
        var totalStories = rng.Next(1, 200);
        var bugs = rng.Next(0, totalStories + 1);
        var overdue = rng.Next(0, totalStories + 1);

        var velocities = Enumerable.Range(0, rng.Next(0, 4))
            .Select(_ => HealthScoreGenerator.CreateVelocity(rng.Next(1, 50), rng.Next(0, 50)))
            .ToList();
        var risks = Enumerable.Range(0, rng.Next(0, 6))
            .Select(i => HealthScoreGenerator.CreateRisk(HealthScoreGenerator.RandomSeverity(seed + i)))
            .ToList();

        var result = (HealthScoreResult)_sut.Calculate(velocities, bugs, totalStories, overdue, totalStories, risks);

        return result.OverallScore >= 0m && result.OverallScore <= 100m;
    }

    // Feature: analytics-reporting, Property 3: When no velocity data, velocityScore = 50 (neutral)
    [Property(MaxTest = 100)]
    public bool NoVelocityData_VelocityScoreIsNeutral(PositiveInt totalStories)
    {
        var total = totalStories.Get % 200 + 1;

        var result = (HealthScoreResult)_sut.Calculate(
            Enumerable.Empty<VelocitySnapshot>(), 0, total, 0, total,
            Enumerable.Empty<RiskRegister>());

        return result.VelocityScore == 50m;
    }

    // Feature: analytics-reporting, Property 4: When no stories, bugRateScore = 50 (neutral)
    [Property(MaxTest = 100)]
    public bool NoStories_BugRateScoreIsNeutral()
    {
        var result = (HealthScoreResult)_sut.Calculate(
            Enumerable.Empty<VelocitySnapshot>(), 0, 0, 0, 0,
            Enumerable.Empty<RiskRegister>());

        return result.BugRateScore == 50m;
    }

    // Feature: analytics-reporting, Property 5: When no risks, riskScore = 100
    [Property(MaxTest = 100)]
    public bool NoRisks_RiskScoreIs100(PositiveInt totalStories)
    {
        var total = totalStories.Get % 200 + 1;

        var result = (HealthScoreResult)_sut.Calculate(
            Enumerable.Empty<VelocitySnapshot>(), 0, total, 0, total,
            Enumerable.Empty<RiskRegister>());

        return result.RiskScore == 100m;
    }

    // Feature: analytics-reporting, Property 6: DetermineTrend: improving when current > previous + 5, declining when current < previous - 5, stable otherwise
    [Property(MaxTest = 100)]
    public bool DetermineTrend_CorrectClassification(ushort seedA, ushort seedB)
    {
        // Use bounded decimals to avoid overflow
        var current = (decimal)(seedA % 200);
        var previous = (decimal)(seedB % 200);

        var trend = _sut.DetermineTrend(current, previous);

        if (current > previous + 5)
            return trend == "improving";
        if (current < previous - 5)
            return trend == "declining";
        return trend == "stable";
    }

    // Feature: analytics-reporting, Property 7: DetermineTrend with null previous returns "stable"
    [Property(MaxTest = 100)]
    public bool DetermineTrend_NullPrevious_ReturnsStable(ushort seed)
    {
        var current = (decimal)(seed % 200);
        return _sut.DetermineTrend(current, null) == "stable";
    }
}
