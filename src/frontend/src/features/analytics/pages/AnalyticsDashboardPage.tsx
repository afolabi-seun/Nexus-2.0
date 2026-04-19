import { useState, useEffect, useCallback } from 'react';
import { useAnalyticsStore } from '@/stores/analyticsStore';
import { workApi } from '@/api/workApi';
import { PageHeader } from '@/components/common/PageHeader';
import { HelpTooltip } from '@/components/common/HelpTooltip';

interface ProjectOption {
    projectId: string;
    name: string;
}

function scoreColor(score: number): string {
    if (score > 70) return 'text-green-600';
    if (score >= 40) return 'text-yellow-500';
    return 'text-destructive';
}

function fmt(n: number) {
    return n.toLocaleString(undefined, { style: 'currency', currency: 'USD' });
}

export function AnalyticsDashboardPage() {
    const [projects, setProjects] = useState<ProjectOption[]>([]);
    const [selectedProjectId, setSelectedProjectId] = useState('');
    const { dashboard, loading, error, fetchDashboard } = useAnalyticsStore();

    const loadProjects = useCallback(async () => {
        try {
            const res = await workApi.getProjects({ page: 1, pageSize: 100 });
            const list = res.data.map((p) => ({ projectId: p.projectId, name: p.name }));
            setProjects(list);
            if (list.length > 0 && !selectedProjectId) {
                setSelectedProjectId(list[0].projectId);
            }
        } catch {
            // non-critical
        }
    }, [selectedProjectId]);

    useEffect(() => {
        loadProjects();
    }, [loadProjects]);

    useEffect(() => {
        if (selectedProjectId) {
            fetchDashboard(selectedProjectId);
        }
    }, [selectedProjectId, fetchDashboard]);

    const healthScore = dashboard?.projectHealth?.overallScore ?? null;
    const trend = dashboard?.projectHealth?.trend ?? 'stable';

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <PageHeader title="Analytics Dashboard" description="Project health, velocity trends, bug metrics, and cost analysis. Select a project to view." dismissKey="analytics" />
                <select
                    value={selectedProjectId}
                    onChange={(e) => setSelectedProjectId(e.target.value)}
                    className="rounded-md border border-input bg-background px-3 py-1.5 text-sm"
                    aria-label="Select project"
                >
                    {projects.map((p) => (
                        <option key={p.projectId} value={p.projectId}>
                            {p.name}
                        </option>
                    ))}
                </select>
            </div>

            {loading && <p className="text-sm text-muted-foreground">Loading dashboard…</p>}
            {error && <p className="text-sm text-destructive">{error}</p>}

            {!loading && !error && dashboard && (
                <>
                    <div className="grid grid-cols-2 gap-4 sm:grid-cols-4 lg:grid-cols-7">
                        <div className="rounded-lg border border-border bg-card p-4">
                            <p className="text-xs text-muted-foreground flex items-center gap-1">Health Score <HelpTooltip text="Composite score (0–100) based on velocity consistency, bug rate, overdue stories, and active risks." /></p>
                            <p className={`mt-1 text-2xl font-semibold ${healthScore != null ? scoreColor(healthScore) : 'text-foreground'}`}>
                                {healthScore != null ? Math.round(healthScore) : '—'}
                            </p>
                            <p className="text-xs text-muted-foreground">{trend}</p>
                        </div>

                        <div className="rounded-lg border border-border bg-card p-4">
                            <p className="text-xs text-muted-foreground flex items-center gap-1">Velocity <HelpTooltip text="Story points completed per sprint. Higher is better, but consistency matters more." /></p>
                            <p className="mt-1 text-2xl font-semibold text-foreground">
                                {dashboard.velocitySnapshot
                                    ? dashboard.velocitySnapshot.completedPoints
                                    : '—'}
                            </p>
                            <p className="text-xs text-muted-foreground">
                                {dashboard.velocitySnapshot
                                    ? `of ${dashboard.velocitySnapshot.committedPoints} committed`
                                    : 'No data'}
                            </p>
                        </div>

                        <div className="rounded-lg border border-border bg-card p-4">
                            <p className="text-xs text-muted-foreground">Active Bugs</p>
                            <p className="mt-1 text-2xl font-semibold text-yellow-600">
                                {dashboard.activeBugCount}
                            </p>
                        </div>

                        <div className="rounded-lg border border-border bg-card p-4">
                            <p className="text-xs text-muted-foreground">Active Risks</p>
                            <p className="mt-1 text-2xl font-semibold text-foreground">
                                {dashboard.activeRiskCount}
                            </p>
                        </div>

                        <div className="rounded-lg border border-border bg-card p-4">
                            <p className="text-xs text-muted-foreground">Blocked Stories</p>
                            <p className={`mt-1 text-2xl font-semibold ${dashboard.blockedStoryCount > 0 ? 'text-destructive' : 'text-foreground'}`}>
                                {dashboard.blockedStoryCount}
                            </p>
                        </div>

                        <div className="rounded-lg border border-border bg-card p-4">
                            <p className="text-xs text-muted-foreground">Total Cost</p>
                            <p className="mt-1 text-2xl font-semibold text-foreground">
                                {fmt(dashboard.totalProjectCost)}
                            </p>
                        </div>

                        <div className="rounded-lg border border-border bg-card p-4">
                            <p className="text-xs text-muted-foreground flex items-center gap-1">Burn Rate / Day <HelpTooltip text="Average daily cost based on billable time entries and cost rates." /></p>
                            <p className="mt-1 text-2xl font-semibold text-foreground">
                                {fmt(dashboard.burnRatePerDay)}
                            </p>
                        </div>
                    </div>
                </>
            )}

            {!loading && !error && !dashboard && selectedProjectId && (
                <p className="text-sm text-muted-foreground">No dashboard data available. Complete a sprint to generate analytics.</p>
            )}
        </div>
    );
}
