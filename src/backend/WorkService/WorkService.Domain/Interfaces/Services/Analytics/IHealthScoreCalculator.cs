using WorkService.Domain.Entities;

namespace WorkService.Domain.Interfaces.Services.Analytics;

public interface IHealthScoreCalculator
{
    /// <summary>
    /// Calculates the composite health score from sub-scores.
    /// Each sub-score is 0–100. Missing data yields a neutral 50.
    /// overallScore = velocity × 0.30 + bugRate × 0.25 + overdue × 0.25 + risk × 0.20
    /// </summary>
    object Calculate(
        IEnumerable<VelocitySnapshot> recentVelocity,
        int openBugCount, int totalActiveStories,
        int overdueStoryCount, int totalStoriesWithDueDate,
        IEnumerable<RiskRegister> activeRisks);

    /// <summary>
    /// Determines trend by comparing current score to previous.
    /// improving: current > previous + 5
    /// declining: current &lt; previous - 5
    /// stable: otherwise
    /// </summary>
    string DetermineTrend(decimal currentScore, decimal? previousScore);
}
