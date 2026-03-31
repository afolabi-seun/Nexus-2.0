import { useState, useEffect } from 'react';
import { useAnalyticsStore } from '@/stores/analyticsStore';

interface ProjectHealthGaugeProps {
    projectId: string;
}

function scoreColor(score: number): string {
    if (score > 70) return 'text-green-600';
    if (score >= 40) return 'text-yellow-500';
    return 'text-destructive';
}

function scoreBg(score: number): string {
    if (score > 70) return 'bg-green-600';
    if (score >= 40) return 'bg-yellow-500';
    return 'bg-destructive';
}

function trendIcon(trend: string): string {
    if (trend === 'improving') return '↑';
    if (trend === 'declining') return '↓';
    return '→';
}

function trendColor(trend: string): string {
    if (trend === 'improving') return 'text-green-600';
    if (trend === 'declining') return 'text-destructive';
    return 'text-muted-foreground';
}

export function ProjectHealthGauge({ projectId }: ProjectHealthGaugeProps) {
    const [showHistory, setShowHistory] = useState(false);
    const { projectHealth, loading, error, fetchProjectHealth } = useAnalyticsStore();

    useEffect(() => {
        fetchProjectHealth(projectId, showHistory);
    }, [projectId, showHistory, fetchProjectHealth]);

    const subScores = projectHealth
        ? [
            { label: 'Velocity', value: projectHealth.velocityScore, weight: '30%' },
            { label: 'Bug Rate', value: projectHealth.bugRateScore, weight: '25%' },
            { label: 'Overdue', value: projectHealth.overdueScore, weight: '25%' },
            { label: 'Risk', value: projectHealth.riskScore, weight: '20%' },
        ]
        : [];

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-foreground">Project Health</h3>
                <button
                    onClick={() => setShowHistory((h) => !h)}
                    className="rounded-md border border-input px-2 py-1 text-xs text-muted-foreground hover:bg-accent"
                >
                    {showHistory ? 'Current' : 'History'}
                </button>
            </div>

            {loading && <p className="text-sm text-muted-foreground">Loading…</p>}
            {error && <p className="text-sm text-destructive">{error}</p>}

            {!loading && !error && !projectHealth && (
                <p className="text-sm text-muted-foreground">No health data available.</p>
            )}

            {!loading && !error && projectHealth && !showHistory && (
                <>
                    <div className="flex items-center gap-4">
                        <div
                            className={`flex h-20 w-20 items-center justify-center rounded-full border-4 ${projectHealth.overallScore > 70
                                    ? 'border-green-600'
                                    : projectHealth.overallScore >= 40
                                        ? 'border-yellow-500'
                                        : 'border-destructive'
                                }`}
                        >
                            <span className={`text-2xl font-bold ${scoreColor(projectHealth.overallScore)}`}>
                                {Math.round(projectHealth.overallScore)}
                            </span>
                        </div>
                        <div>
                            <p className={`text-lg font-medium ${trendColor(projectHealth.trend)}`}>
                                {trendIcon(projectHealth.trend)} {projectHealth.trend}
                            </p>
                            <p className="text-xs text-muted-foreground">
                                {new Date(projectHealth.snapshotDate).toLocaleDateString()}
                            </p>
                        </div>
                    </div>

                    <div className="grid grid-cols-2 gap-3">
                        {subScores.map((s) => (
                            <div key={s.label} className="space-y-1">
                                <div className="flex items-center justify-between text-xs">
                                    <span className="text-muted-foreground">
                                        {s.label} ({s.weight})
                                    </span>
                                    <span className={scoreColor(s.value)}>
                                        {Math.round(s.value)}
                                    </span>
                                </div>
                                <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                                    <div
                                        className={`h-full rounded-full ${scoreBg(s.value)}`}
                                        style={{ width: `${Math.min(s.value, 100)}%` }}
                                    />
                                </div>
                            </div>
                        ))}
                    </div>
                </>
            )}

            {!loading && !error && projectHealth && showHistory && projectHealth.history && (
                <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                        <thead>
                            <tr className="border-b border-border text-left text-xs text-muted-foreground">
                                <th className="pb-2 pr-4">Date</th>
                                <th className="pb-2 pr-4 text-right">Overall</th>
                                <th className="pb-2 pr-4 text-right">Velocity</th>
                                <th className="pb-2 pr-4 text-right">Bug Rate</th>
                                <th className="pb-2 pr-4 text-right">Overdue</th>
                                <th className="pb-2 pr-4 text-right">Risk</th>
                                <th className="pb-2 text-right">Trend</th>
                            </tr>
                        </thead>
                        <tbody>
                            {projectHealth.history.map((h, i) => (
                                <tr key={i} className="border-b border-border/50">
                                    <td className="py-1.5 pr-4">
                                        {new Date(h.snapshotDate).toLocaleDateString()}
                                    </td>
                                    <td className={`py-1.5 pr-4 text-right font-medium ${scoreColor(h.overallScore)}`}>
                                        {Math.round(h.overallScore)}
                                    </td>
                                    <td className="py-1.5 pr-4 text-right">{Math.round(h.velocityScore)}</td>
                                    <td className="py-1.5 pr-4 text-right">{Math.round(h.bugRateScore)}</td>
                                    <td className="py-1.5 pr-4 text-right">{Math.round(h.overdueScore)}</td>
                                    <td className="py-1.5 pr-4 text-right">{Math.round(h.riskScore)}</td>
                                    <td className={`py-1.5 text-right ${trendColor(h.trend)}`}>
                                        {trendIcon(h.trend)} {h.trend}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}
