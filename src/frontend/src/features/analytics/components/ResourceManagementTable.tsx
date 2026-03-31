import { useState, useEffect } from 'react';
import { useAnalyticsStore } from '@/stores/analyticsStore';

export function ResourceManagementTable() {
    const [dateFrom, setDateFrom] = useState('');
    const [dateTo, setDateTo] = useState('');
    const [departmentId, setDepartmentId] = useState('');
    const [expandedMember, setExpandedMember] = useState<string | null>(null);
    const { resourceManagement, loading, error, fetchResourceManagement } = useAnalyticsStore();

    useEffect(() => {
        fetchResourceManagement(dateFrom || undefined, dateTo || undefined, departmentId || undefined);
    }, [dateFrom, dateTo, departmentId, fetchResourceManagement]);

    const toggleExpand = (memberId: string) => {
        setExpandedMember((prev) => (prev === memberId ? null : memberId));
    };

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-foreground">Resource Management</h3>
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
                <label className="space-y-1">
                    <span className="text-xs text-muted-foreground">Department ID</span>
                    <input
                        type="text"
                        value={departmentId}
                        onChange={(e) => setDepartmentId(e.target.value)}
                        placeholder="Optional"
                        className="block rounded-md border border-input bg-background px-2 py-1 text-sm"
                    />
                </label>
            </div>

            {loading && <p className="text-sm text-muted-foreground">Loading…</p>}
            {error && <p className="text-sm text-destructive">{error}</p>}

            {!loading && !error && resourceManagement.length === 0 && (
                <p className="text-sm text-muted-foreground">No resource data available.</p>
            )}

            {!loading && !error && resourceManagement.length > 0 && (
                <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                        <thead>
                            <tr className="border-b border-border text-left text-xs font-medium text-muted-foreground">
                                <th className="pb-2 pr-4" />
                                <th className="pb-2 pr-4">Member</th>
                                <th className="pb-2 pr-4 text-right">Hours</th>
                                <th className="pb-2 pr-4 text-right">Utilization</th>
                                <th className="pb-2 text-right">Projects</th>
                            </tr>
                        </thead>
                        <tbody>
                            {resourceManagement.map((m) => (
                                <>
                                    <tr
                                        key={m.memberId}
                                        className="cursor-pointer border-b border-border/50 hover:bg-accent/50"
                                        onClick={() => toggleExpand(m.memberId)}
                                    >
                                        <td className="py-2 pr-2 text-muted-foreground">
                                            {expandedMember === m.memberId ? '▾' : '▸'}
                                        </td>
                                        <td className="py-2 pr-4 font-medium text-foreground">
                                            {m.memberName}
                                        </td>
                                        <td className="py-2 pr-4 text-right">
                                            {m.totalLoggedHours.toFixed(1)}
                                        </td>
                                        <td className="py-2 pr-4 text-right">
                                            <div className="flex items-center justify-end gap-2">
                                                <div className="h-2 w-16 overflow-hidden rounded-full bg-muted">
                                                    <div
                                                        className="h-full rounded-full bg-primary"
                                                        style={{
                                                            width: `${Math.min(m.capacityUtilizationPercentage, 100)}%`,
                                                        }}
                                                    />
                                                </div>
                                                <span>{m.capacityUtilizationPercentage.toFixed(0)}%</span>
                                            </div>
                                        </td>
                                        <td className="py-2 text-right">
                                            {m.projectBreakdown.length}
                                        </td>
                                    </tr>
                                    {expandedMember === m.memberId &&
                                        m.projectBreakdown.map((p) => (
                                            <tr
                                                key={`${m.memberId}-${p.projectId}`}
                                                className="border-b border-border/30 bg-accent/30"
                                            >
                                                <td />
                                                <td className="py-1.5 pl-6 pr-4 text-muted-foreground">
                                                    {p.projectName}
                                                </td>
                                                <td className="py-1.5 pr-4 text-right text-muted-foreground">
                                                    {p.hoursLogged.toFixed(1)}
                                                </td>
                                                <td className="py-1.5 pr-4 text-right text-muted-foreground">
                                                    {p.percentage.toFixed(0)}%
                                                </td>
                                                <td />
                                            </tr>
                                        ))}
                                </>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}
