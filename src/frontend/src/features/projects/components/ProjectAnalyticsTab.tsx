import { useState, useEffect, useCallback } from 'react';
import { analyticsApi } from '@/api/analyticsApi';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import type {
    ProjectHealthResponse,
    VelocitySnapshotResponse,
    BugMetricsResponse,
} from '@/types/analytics';
import {
    BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts';

interface Props {
    projectId: string;
}

function scoreColor(score: number): string {
    if (score > 70) return 'text-green-600';
    if (score >= 40) return 'text-yellow-500';
    return 'text-destructive';
}

export function ProjectAnalyticsTab({ projectId }: Props) {
    const [health, setHealth] = useState<ProjectHealthResponse | null>(null);
    const [velocity, setVelocity] = useState<VelocitySnapshotResponse[]>([]);
    const [bugs, setBugs] = useState<BugMetricsResponse | null>(null);
    const [loading, setLoading] = useState(true);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const [h, v, b] = await Promise.all([
                analyticsApi.getProjectHealth({ projectId, history: true }).catch(() => null),
                analyticsApi.getVelocityTrends({ projectId, sprintCount: 10 }).catch(() => []),
                analyticsApi.getBugMetrics({ projectId }).catch(() => null),
            ]);
            setHealth(h);
            setVelocity(v);
            setBugs(b);
        } finally {
            setLoading(false);
        }
    }, [projectId]);

    useEffect(() => { load(); }, [load]);

    if (loading) return <SkeletonLoader variant="card" />;

    return (
        <div className="space-y-6">
            {/* Health Scores */}
            {health && (
                <section className="space-y-2">
                    <h3 className="text-sm font-semibold text-foreground">Project Health</h3>
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
                        <ScoreCard label="Overall" score={health.overallScore} trend={health.trend} />
                        <ScoreCard label="Velocity" score={health.velocityScore} />
                        <ScoreCard label="Bug Rate" score={health.bugRateScore} />
                        <ScoreCard label="Overdue" score={health.overdueScore} />
                        <ScoreCard label="Risk" score={health.riskScore} />
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Snapshot</p>
                            <p className="mt-1 text-sm text-foreground">
                                {new Date(health.snapshotDate).toLocaleDateString()}
                            </p>
                        </div>
                    </div>
                </section>
            )}

            {/* Velocity Chart */}
            {velocity.length > 0 && (
                <section className="space-y-2">
                    <h3 className="text-sm font-semibold text-foreground">Velocity Trends</h3>
                    <div className="h-64 rounded-lg border border-border bg-card p-4">
                        <ResponsiveContainer width="100%" height="100%">
                            <BarChart data={velocity}>
                                <CartesianGrid strokeDasharray="3 3" />
                                <XAxis dataKey="sprintName" tick={{ fontSize: 12 }} />
                                <YAxis tick={{ fontSize: 12 }} />
                                <Tooltip />
                                <Bar dataKey="committedPoints" fill="hsl(var(--muted-foreground))" name="Committed" />
                                <Bar dataKey="completedPoints" fill="hsl(var(--primary))" name="Completed" />
                            </BarChart>
                        </ResponsiveContainer>
                    </div>
                </section>
            )}

            {/* Bug Metrics */}
            {bugs && (
                <section className="space-y-2">
                    <h3 className="text-sm font-semibold text-foreground">Bug Metrics</h3>
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                        <MetricCard label="Total Bugs" value={bugs.totalBugs} />
                        <MetricCard label="Open" value={bugs.openBugs} highlight={bugs.openBugs > 0} />
                        <MetricCard label="Closed" value={bugs.closedBugs} />
                        <MetricCard label="Bug Rate" value={`${bugs.bugRate}%`} />
                    </div>
                </section>
            )}

            {!health && velocity.length === 0 && !bugs && (
                <p className="py-8 text-center text-sm text-muted-foreground">
                    No analytics data yet. Complete a sprint to generate health scores and velocity trends.
                </p>
            )}
        </div>
    );
}

function ScoreCard({ label, score, trend }: { label: string; score: number; trend?: string }) {
    return (
        <div className="rounded-lg border border-border bg-card p-3">
            <p className="text-xs text-muted-foreground">{label}</p>
            <p className={`mt-1 text-xl font-semibold ${scoreColor(score)}`}>{Math.round(score)}</p>
            {trend && <p className="text-xs text-muted-foreground">{trend}</p>}
        </div>
    );
}

function MetricCard({ label, value, highlight }: { label: string; value: string | number; highlight?: boolean }) {
    return (
        <div className="rounded-lg border border-border bg-card p-3">
            <p className="text-xs text-muted-foreground">{label}</p>
            <p className={`mt-1 text-lg font-semibold ${highlight ? 'text-yellow-600' : 'text-foreground'}`}>{value}</p>
        </div>
    );
}
