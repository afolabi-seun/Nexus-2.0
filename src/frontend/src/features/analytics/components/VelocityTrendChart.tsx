import { useState, useEffect } from 'react';
import { useAnalyticsStore } from '@/stores/analyticsStore';

interface VelocityTrendChartProps {
    projectId: string;
}

const SPRINT_COUNT_OPTIONS = [5, 10, 20, 50];

export function VelocityTrendChart({ projectId }: VelocityTrendChartProps) {
    const [sprintCount, setSprintCount] = useState(10);
    const { velocityTrends, loading, error, fetchVelocityTrends } = useAnalyticsStore();

    useEffect(() => {
        fetchVelocityTrends(projectId, sprintCount);
    }, [projectId, sprintCount, fetchVelocityTrends]);

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-foreground">Velocity Trends</h3>
                <select
                    value={sprintCount}
                    onChange={(e) => setSprintCount(Number(e.target.value))}
                    className="rounded-md border border-input bg-background px-2 py-1 text-sm"
                    aria-label="Sprint count"
                >
                    {SPRINT_COUNT_OPTIONS.map((n) => (
                        <option key={n} value={n}>
                            Last {n} sprints
                        </option>
                    ))}
                </select>
            </div>

            {loading && <p className="text-sm text-muted-foreground">Loading…</p>}
            {error && <p className="text-sm text-destructive">{error}</p>}

            {!loading && !error && velocityTrends.length === 0 && (
                <p className="text-sm text-muted-foreground">No velocity data available.</p>
            )}

            {!loading && !error && velocityTrends.length > 0 && (
                <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                        <thead>
                            <tr className="border-b border-border text-left text-xs font-medium text-muted-foreground">
                                <th className="pb-2 pr-4">Sprint</th>
                                <th className="pb-2 pr-4 text-right">Committed</th>
                                <th className="pb-2 pr-4 text-right">Completed</th>
                                <th className="pb-2 pr-4 text-right">Completion %</th>
                                <th className="pb-2 pr-4 text-right">Hours</th>
                                <th className="pb-2 text-right">Avg Hrs/Pt</th>
                            </tr>
                        </thead>
                        <tbody>
                            {velocityTrends.map((v) => {
                                const pct =
                                    v.committedPoints > 0
                                        ? Math.round((v.completedPoints / v.committedPoints) * 100)
                                        : 0;
                                return (
                                    <tr key={v.sprintId} className="border-b border-border/50">
                                        <td className="py-2 pr-4 font-medium text-foreground">
                                            {v.sprintName}
                                        </td>
                                        <td className="py-2 pr-4 text-right">{v.committedPoints}</td>
                                        <td className="py-2 pr-4 text-right">{v.completedPoints}</td>
                                        <td className="py-2 pr-4 text-right">
                                            <div className="flex items-center justify-end gap-2">
                                                <div className="h-2 w-16 overflow-hidden rounded-full bg-muted">
                                                    <div
                                                        className="h-full rounded-full bg-primary"
                                                        style={{ width: `${Math.min(pct, 100)}%` }}
                                                    />
                                                </div>
                                                <span>{pct}%</span>
                                            </div>
                                        </td>
                                        <td className="py-2 pr-4 text-right">
                                            {v.totalLoggedHours.toFixed(1)}
                                        </td>
                                        <td className="py-2 text-right">
                                            {v.averageHoursPerPoint != null
                                                ? v.averageHoursPerPoint.toFixed(1)
                                                : '—'}
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}
