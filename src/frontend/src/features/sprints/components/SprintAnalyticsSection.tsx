import { useState, useEffect, useCallback } from 'react';
import { analyticsApi } from '@/api/analyticsApi';
import { timeTrackingApi } from '@/api/timeTrackingApi';
import type { BugMetricsResponse } from '@/types/analytics';

interface Props {
    projectId: string;
    sprintId: string;
}

export function SprintAnalyticsSection({ projectId, sprintId }: Props) {
    const [bugs, setBugs] = useState<BugMetricsResponse | null>(null);
    const [totalHours, setTotalHours] = useState(0);
    const [billableHours, setBillableHours] = useState(0);
    const [entryCount, setEntryCount] = useState(0);
    const [loading, setLoading] = useState(true);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const [bugData, timeData] = await Promise.all([
                analyticsApi.getBugMetrics({ projectId, sprintId }).catch(() => null),
                timeTrackingApi.listTimeEntries({ sprintId, pageSize: 500 }).catch(() => ({ data: [], totalCount: 0 })),
            ]);
            setBugs(bugData);
            const entries = timeData.data;
            setTotalHours(entries.reduce((s, e) => s + e.hours, 0));
            setBillableHours(entries.filter((e) => e.billable).reduce((s, e) => s + e.hours, 0));
            setEntryCount(timeData.totalCount);
        } finally {
            setLoading(false);
        }
    }, [projectId, sprintId]);

    useEffect(() => { load(); }, [load]);

    if (loading) return null;
    if (!bugs && entryCount === 0) return null;

    return (
        <div className="space-y-4">
            {/* Time Summary */}
            {entryCount > 0 && (
                <section className="space-y-2">
                    <h3 className="text-sm font-semibold text-foreground">Time Logged</h3>
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                        <Card label="Total Hours" value={`${totalHours.toFixed(1)}h`} />
                        <Card label="Billable" value={`${billableHours.toFixed(1)}h`} />
                        <Card label="Entries" value={entryCount} />
                    </div>
                </section>
            )}

            {/* Bug Metrics */}
            {bugs && (bugs.totalBugs > 0 || bugs.openBugs > 0) && (
                <section className="space-y-2">
                    <h3 className="text-sm font-semibold text-foreground">Bug Metrics</h3>
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                        <Card label="Total Bugs" value={bugs.totalBugs} />
                        <Card label="Open" value={bugs.openBugs} highlight={bugs.openBugs > 0} />
                        <Card label="Closed" value={bugs.closedBugs} />
                        <Card label="Bug Rate" value={`${bugs.bugRate}%`} />
                    </div>
                </section>
            )}
        </div>
    );
}

function Card({ label, value, highlight }: { label: string; value: string | number; highlight?: boolean }) {
    return (
        <div className="rounded-lg border border-border bg-card p-3">
            <p className="text-xs text-muted-foreground">{label}</p>
            <p className={`mt-1 text-lg font-semibold ${highlight ? 'text-yellow-600' : 'text-foreground'}`}>{value}</p>
        </div>
    );
}
