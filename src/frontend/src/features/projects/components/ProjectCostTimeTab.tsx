import { useState, useEffect, useCallback } from 'react';
import { timeTrackingApi } from '@/api/timeTrackingApi';
import { analyticsApi } from '@/api/analyticsApi';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { Pagination } from '@/components/common/Pagination';
import { Badge } from '@/components/common/Badge';
import { usePagination } from '@/hooks/usePagination';
import type { ProjectCostAnalyticsResponse } from '@/types/analytics';
import type { TimeEntryResponse } from '@/types/timeTracking';

interface Props {
    projectId: string;
}

function fmt(n: number) {
    return n.toLocaleString(undefined, { style: 'currency', currency: 'USD' });
}

export function ProjectCostTimeTab({ projectId }: Props) {
    const [cost, setCost] = useState<ProjectCostAnalyticsResponse | null>(null);
    const [timeEntries, setTimeEntries] = useState<TimeEntryResponse[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [loading, setLoading] = useState(true);
    const { page, pageSize, setPage, setPageSize } = usePagination(1, 10);

    const loadCost = useCallback(async () => {
        try {
            const data = await analyticsApi.getProjectCost({ projectId });
            setCost(data);
        } catch {
            // non-critical
        }
    }, [projectId]);

    const loadTimeEntries = useCallback(async () => {
        try {
            const res = await timeTrackingApi.listTimeEntries({ projectId, page, pageSize });
            setTimeEntries(res.data);
            setTotalCount(res.totalCount);
        } catch {
            // non-critical
        }
    }, [projectId, page, pageSize]);

    useEffect(() => {
        setLoading(true);
        Promise.all([loadCost(), loadTimeEntries()]).finally(() => setLoading(false));
    }, [loadCost, loadTimeEntries]);

    if (loading) return <SkeletonLoader variant="card" />;

    return (
        <div className="space-y-6">
            {/* Cost Summary */}
            {cost && (
                <section className="space-y-2">
                    <h3 className="text-sm font-semibold text-foreground">Cost Summary</h3>
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                        <CostCard label="Total Cost" value={fmt(cost.totalCost)} />
                        <CostCard label="Burn Rate / Day" value={fmt(cost.burnRatePerDay)} />
                        <CostCard label="Billable Hours" value={`${cost.totalBillableHours.toFixed(1)}h`} />
                        <CostCard label="Non-Billable Hours" value={`${cost.totalNonBillableHours.toFixed(1)}h`} />
                    </div>

                    {/* Cost by Department */}
                    {cost.costByDepartment.length > 0 && (
                        <div className="mt-3">
                            <p className="text-xs font-medium text-muted-foreground mb-2">Cost by Department</p>
                            <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                                {cost.costByDepartment.map((d) => (
                                    <div key={d.departmentId} className="rounded border border-border px-3 py-2">
                                        <p className="text-xs font-medium text-foreground">{d.departmentName || 'Unknown'}</p>
                                        <p className="text-sm text-muted-foreground">{fmt(d.cost)} · {d.hours.toFixed(1)}h</p>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}
                </section>
            )}

            {/* Time Entries */}
            <section className="space-y-2">
                <h3 className="text-sm font-semibold text-foreground">Time Entries</h3>
                {timeEntries.length === 0 ? (
                    <p className="py-4 text-center text-sm text-muted-foreground">
                        No time entries logged for this project yet.
                    </p>
                ) : (
                    <>
                        <div className="space-y-1.5">
                            {timeEntries.map((entry) => (
                                <div key={entry.timeEntryId} className="flex items-center justify-between rounded-md border border-border px-3 py-2">
                                    <div className="flex items-center gap-2 min-w-0">
                                        <span className="text-sm text-foreground truncate">{entry.storyTitle || entry.storyId}</span>
                                        <Badge variant="status" value={entry.status} />
                                    </div>
                                    <div className="flex items-center gap-3 shrink-0 text-sm text-muted-foreground">
                                        <span>{entry.hours}h</span>
                                        <span>{new Date(entry.date).toLocaleDateString()}</span>
                                        {entry.billable && <span className="text-xs text-green-600">Billable</span>}
                                    </div>
                                </div>
                            ))}
                        </div>
                        <Pagination
                            page={page}
                            pageSize={pageSize}
                            totalCount={totalCount}
                            onPageChange={setPage}
                            onPageSizeChange={(s) => { setPageSize(s); setPage(1); }}
                        />
                    </>
                )}
            </section>

            {!cost && timeEntries.length === 0 && (
                <p className="py-8 text-center text-sm text-muted-foreground">
                    No cost or time data yet. Log time entries against stories in this project.
                </p>
            )}
        </div>
    );
}

function CostCard({ label, value }: { label: string; value: string }) {
    return (
        <div className="rounded-lg border border-border bg-card p-3">
            <p className="text-xs text-muted-foreground">{label}</p>
            <p className="mt-1 text-lg font-semibold text-foreground">{value}</p>
        </div>
    );
}
