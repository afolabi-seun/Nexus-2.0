import { useState, useEffect } from 'react';
import { useAnalyticsStore } from '@/stores/analyticsStore';

interface ProjectCostDashboardProps {
    projectId: string;
}

export function ProjectCostDashboard({ projectId }: ProjectCostDashboardProps) {
    const [dateFrom, setDateFrom] = useState('');
    const [dateTo, setDateTo] = useState('');
    const { projectCost, loading, error, fetchProjectCost } = useAnalyticsStore();

    useEffect(() => {
        fetchProjectCost(projectId, dateFrom || undefined, dateTo || undefined);
    }, [projectId, dateFrom, dateTo, fetchProjectCost]);

    const fmt = (n: number) =>
        n.toLocaleString(undefined, { style: 'currency', currency: 'USD' });

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-foreground">Project Cost</h3>
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

            {!loading && !error && projectCost && (
                <>
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Total Cost</p>
                            <p className="mt-1 text-lg font-semibold text-foreground">
                                {fmt(projectCost.totalCost)}
                            </p>
                        </div>
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Burn Rate / Day</p>
                            <p className="mt-1 text-lg font-semibold text-foreground">
                                {fmt(projectCost.burnRatePerDay)}
                            </p>
                        </div>
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Billable Hours</p>
                            <p className="mt-1 text-lg font-semibold text-foreground">
                                {projectCost.totalBillableHours.toFixed(1)}
                            </p>
                        </div>
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Non-Billable Hours</p>
                            <p className="mt-1 text-lg font-semibold text-foreground">
                                {projectCost.totalNonBillableHours.toFixed(1)}
                            </p>
                        </div>
                    </div>

                    {projectCost.costByMember.length > 0 && (
                        <div>
                            <h4 className="mb-2 text-xs font-medium text-muted-foreground">
                                Cost by Member
                            </h4>
                            <table className="w-full text-sm">
                                <thead>
                                    <tr className="border-b border-border text-left text-xs text-muted-foreground">
                                        <th className="pb-2 pr-4">Member</th>
                                        <th className="pb-2 pr-4 text-right">Hours</th>
                                        <th className="pb-2 text-right">Cost</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {projectCost.costByMember.map((c) => (
                                        <tr key={c.memberId} className="border-b border-border/50">
                                            <td className="py-1.5 pr-4">{c.memberName}</td>
                                            <td className="py-1.5 pr-4 text-right">
                                                {c.hours.toFixed(1)}
                                            </td>
                                            <td className="py-1.5 text-right">{fmt(c.cost)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}

                    {projectCost.costByDepartment.length > 0 && (
                        <div>
                            <h4 className="mb-2 text-xs font-medium text-muted-foreground">
                                Cost by Department
                            </h4>
                            <table className="w-full text-sm">
                                <thead>
                                    <tr className="border-b border-border text-left text-xs text-muted-foreground">
                                        <th className="pb-2 pr-4">Department</th>
                                        <th className="pb-2 pr-4 text-right">Hours</th>
                                        <th className="pb-2 text-right">Cost</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {projectCost.costByDepartment.map((d) => (
                                        <tr key={d.departmentId} className="border-b border-border/50">
                                            <td className="py-1.5 pr-4">{d.departmentName}</td>
                                            <td className="py-1.5 pr-4 text-right">
                                                {d.hours.toFixed(1)}
                                            </td>
                                            <td className="py-1.5 text-right">{fmt(d.cost)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}
