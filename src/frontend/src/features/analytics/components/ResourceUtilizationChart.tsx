import { useState, useEffect } from 'react';
import { useAnalyticsStore } from '@/stores/analyticsStore';

interface ResourceUtilizationChartProps {
    projectId: string;
}

export function ResourceUtilizationChart({ projectId }: ResourceUtilizationChartProps) {
    const [dateFrom, setDateFrom] = useState('');
    const [dateTo, setDateTo] = useState('');
    const { resourceUtilization, loading, error, fetchResourceUtilization } = useAnalyticsStore();

    useEffect(() => {
        fetchResourceUtilization(projectId, dateFrom || undefined, dateTo || undefined);
    }, [projectId, dateFrom, dateTo, fetchResourceUtilization]);

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-foreground">Resource Utilization</h3>
            </div>

            <div className="flex flex-wrap items-end gap-3">
                <label className="space-y-1">
                    <span className="text-xs text-muted-foreground">From</span>
                    <input
                        type="date"
                        value={dateFrom}
                        onChange={(e) => setDateFrom(e.target.value)}
                        className="block rounded-md border border-input bg-background px-2 py-1 text-sm"
                    />
                </label>
                <label className="space-y-1">
                    <span className="text-xs text-muted-foreground">To</span>
                    <input
                        type="date"
                        value={dateTo}
                        onChange={(e) => setDateTo(e.target.value)}
                        className="block rounded-md border border-input bg-background px-2 py-1 text-sm"
                    />
                </label>
            </div>

            {loading && <p className="text-sm text-muted-foreground">Loading…</p>}
            {error && <p className="text-sm text-destructive">{error}</p>}

            {!loading && !error && resourceUtilization.length === 0 && (
                <p className="text-sm text-muted-foreground">No utilization data available.</p>
            )}

            {!loading && !error && resourceUtilization.length > 0 && (
                <div className="space-y-3">
                    {resourceUtilization.map((m) => (
                        <div key={m.memberId} className="space-y-1">
                            <div className="flex items-center justify-between text-sm">
                                <span className="font-medium text-foreground">{m.memberName}</span>
                                <span className="text-muted-foreground">
                                    {m.utilizationPercentage.toFixed(0)}%
                                </span>
                            </div>
                            <div className="h-3 w-full overflow-hidden rounded-full bg-muted">
                                <div
                                    className={`h-full rounded-full ${m.utilizationPercentage > 100
                                            ? 'bg-destructive'
                                            : m.utilizationPercentage > 80
                                                ? 'bg-yellow-500'
                                                : 'bg-primary'
                                        }`}
                                    style={{ width: `${Math.min(m.utilizationPercentage, 100)}%` }}
                                />
                            </div>
                            <div className="flex gap-4 text-xs text-muted-foreground">
                                <span>Logged: {m.totalLoggedHours.toFixed(1)}h</span>
                                <span>Expected: {m.expectedHours.toFixed(1)}h</span>
                                <span>Billable: {m.billableHours.toFixed(1)}h</span>
                                <span>Non-billable: {m.nonBillableHours.toFixed(1)}h</span>
                                {m.overtimeHours > 0 && (
                                    <span className="text-destructive">
                                        OT: {m.overtimeHours.toFixed(1)}h
                                    </span>
                                )}
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
