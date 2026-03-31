import { useState, useEffect } from 'react';
import { useAnalyticsStore } from '@/stores/analyticsStore';

interface BugMetricsPanelProps {
    projectId: string;
}

export function BugMetricsPanel({ projectId }: BugMetricsPanelProps) {
    const [sprintId, setSprintId] = useState('');
    const { bugMetrics, loading, error, fetchBugMetrics } = useAnalyticsStore();

    useEffect(() => {
        fetchBugMetrics(projectId, sprintId || undefined);
    }, [projectId, sprintId, fetchBugMetrics]);

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-foreground">Bug Metrics</h3>
                <input
                    type="text"
                    placeholder="Sprint ID (optional)"
                    value={sprintId}
                    onChange={(e) => setSprintId(e.target.value)}
                    className="rounded-md border border-input bg-background px-2 py-1 text-sm"
                />
            </div>

            {loading && <p className="text-sm text-muted-foreground">Loading…</p>}
            {error && <p className="text-sm text-destructive">{error}</p>}

            {!loading && !error && !bugMetrics && (
                <p className="text-sm text-muted-foreground">No bug data available.</p>
            )}

            {!loading && !error && bugMetrics && (
                <>
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-5">
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Total</p>
                            <p className="mt-1 text-lg font-semibold text-foreground">
                                {bugMetrics.totalBugs}
                            </p>
                        </div>
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Open</p>
                            <p className="mt-1 text-lg font-semibold text-yellow-600">
                                {bugMetrics.openBugs}
                            </p>
                        </div>
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Closed</p>
                            <p className="mt-1 text-lg font-semibold text-green-600">
                                {bugMetrics.closedBugs}
                            </p>
                        </div>
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Reopened</p>
                            <p className="mt-1 text-lg font-semibold text-destructive">
                                {bugMetrics.reopenedBugs}
                            </p>
                        </div>
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Bug Rate</p>
                            <p className="mt-1 text-lg font-semibold text-foreground">
                                {bugMetrics.bugRate.toFixed(1)}%
                            </p>
                        </div>
                    </div>

                    {Object.keys(bugMetrics.bugsBySeverity).length > 0 && (
                        <div>
                            <h4 className="mb-2 text-xs font-medium text-muted-foreground">
                                By Severity
                            </h4>
                            <div className="space-y-2">
                                {Object.entries(bugMetrics.bugsBySeverity).map(([sev, count]) => (
                                    <div key={sev} className="flex items-center gap-3">
                                        <span className="w-20 text-xs text-muted-foreground">{sev}</span>
                                        <div className="h-2 flex-1 overflow-hidden rounded-full bg-muted">
                                            <div
                                                className="h-full rounded-full bg-primary"
                                                style={{
                                                    width: `${bugMetrics.totalBugs > 0 ? (count / bugMetrics.totalBugs) * 100 : 0}%`,
                                                }}
                                            />
                                        </div>
                                        <span className="w-8 text-right text-xs font-medium">{count}</span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}

                    {bugMetrics.bugTrend.length > 0 && (
                        <div>
                            <h4 className="mb-2 text-xs font-medium text-muted-foreground">
                                Bug Trend (Last Sprints)
                            </h4>
                            <div className="flex items-end gap-1" style={{ height: 80 }}>
                                {bugMetrics.bugTrend.map((t) => {
                                    const max = Math.max(...bugMetrics.bugTrend.map((b) => b.bugCount), 1);
                                    const h = (t.bugCount / max) * 100;
                                    return (
                                        <div
                                            key={t.sprintId}
                                            className="group relative flex-1"
                                            title={`${t.sprintName}: ${t.bugCount} bugs`}
                                        >
                                            <div
                                                className="w-full rounded-t bg-primary/70"
                                                style={{ height: `${h}%` }}
                                            />
                                            <span className="absolute -top-5 left-1/2 hidden -translate-x-1/2 text-[10px] text-muted-foreground group-hover:block">
                                                {t.bugCount}
                                            </span>
                                        </div>
                                    );
                                })}
                            </div>
                            <div className="mt-1 flex gap-1">
                                {bugMetrics.bugTrend.map((t) => (
                                    <div
                                        key={t.sprintId}
                                        className="flex-1 truncate text-center text-[9px] text-muted-foreground"
                                        title={t.sprintName}
                                    >
                                        {t.sprintName}
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}
