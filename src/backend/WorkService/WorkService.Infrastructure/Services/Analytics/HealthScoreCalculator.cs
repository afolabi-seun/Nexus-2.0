using WorkService.Application.DTOs.Analytics;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Services.Analytics;

namespace WorkService.Infrastructure.Services.Analytics;

public class HealthScoreCalculator : IHealthScoreCalculator
{
    private const decimal NeutralScore = 50m;
    private const decimal MaxExpectedRisk = 20m;

    private static readonly Dictionary<string, int> SeverityWeights = new()
    {
        ["Critical"] = 4,
        ["High"] = 3,
        ["Medium"] = 2,
        ["Low"] = 1
    };

    public object Calculate(
        IEnumerable<VelocitySnapshot> recentVelocity,
        int openBugCount, int totalActiveStories,
        int overdueStoryCount, int totalStoriesWithDueDate,
        IEnumerable<RiskRegister> activeRisks)
    {
        var velocityScore = CalculateVelocityScore(recentVelocity);
        var bugRateScore = CalculateBugRateScore(openBugCount, totalActiveStories);
        var overdueScore = CalculateOverdueScore(overdueStoryCount, totalStoriesWithDueDate);
        var riskScore = CalculateRiskScore(activeRisks);

        var overallScore = Math.Round(
            velocityScore * 0.30m +
            bugRateScore * 0.25m +
            overdueScore * 0.25m +
            riskScore * 0.20m, 2);

        return new HealthScoreResult
        {
            OverallScore = overallScore,
            VelocityScore = velocityScore,
            BugRateScore = bugRateScore,
            OverdueScore = overdueScore,
            RiskScore = riskScore
        };
    }

    public string DetermineTrend(decimal currentScore, decimal? previousScore)
    {
        if (previousScore == null)
            return "stable";

        if (currentScore > previousScore.Value + 5)
            return "improving";

        if (currentScore < previousScore.Value - 5)
            return "declining";

        return "stable";
    }

    private static decimal CalculateVelocityScore(IEnumerable<VelocitySnapshot> recentVelocity)
    {
        var snapshots = recentVelocity.Take(3).ToList();
        if (snapshots.Count == 0)
            return NeutralScore;

        var ratios = snapshots
            .Where(s => s.CommittedPoints > 0)
            .Select(s => Math.Min(1.0m, (decimal)s.CompletedPoints / s.CommittedPoints))
            .ToList();

        if (ratios.Count == 0)
            return NeutralScore;

        return Math.Round(ratios.Average() * 100m, 2);
    }

    private static decimal CalculateBugRateScore(int openBugCount, int totalActiveStories)
    {
        if (totalActiveStories == 0)
            return NeutralScore;

        var bugRate = (decimal)openBugCount / totalActiveStories * 100m;
        return Math.Round(Math.Max(0m, 100m - bugRate), 2);
    }

    private static decimal CalculateOverdueScore(int overdueStoryCount, int totalStoriesWithDueDate)
    {
        if (totalStoriesWithDueDate == 0)
            return NeutralScore;

        var overdueRate = (decimal)overdueStoryCount / totalStoriesWithDueDate * 100m;
        return Math.Round(Math.Max(0m, 100m - overdueRate), 2);
    }

    private static decimal CalculateRiskScore(IEnumerable<RiskRegister> activeRisks)
    {
        var riskList = activeRisks.ToList();
        if (riskList.Count == 0)
            return 100m;

        var weightedSum = riskList.Sum(r =>
            SeverityWeights.TryGetValue(r.Severity, out var weight) ? weight : 1);

        var score = Math.Max(0m, 100m - ((decimal)weightedSum / MaxExpectedRisk * 100m));
        return Math.Round(score, 2);
    }
}
