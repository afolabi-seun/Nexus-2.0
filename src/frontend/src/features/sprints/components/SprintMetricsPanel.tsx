import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { HelpTooltip } from '@/components/common/HelpTooltip';
import type { SprintMetrics } from '@/types/work';

interface SprintMetricsPanelProps {
    metrics: SprintMetrics;
}

export function SprintMetricsPanel({ metrics }: SprintMetricsPanelProps) {
    const storiesByStatusData = Object.entries(metrics.storiesByStatus).map(([status, count]) => ({
        name: status,
        count,
    }));

    const tasksByDeptData = Object.entries(metrics.tasksByDepartment).map(([dept, count]) => ({
        name: dept,
        count,
    }));

    return (
        <div className="space-y-4">
            {/* Summary cards */}
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
                <MetricCard label="Total Stories" value={metrics.totalStories} />
                <MetricCard label="Completed" value={metrics.completedStories} />
                <MetricCard label="Total Points" value={metrics.totalStoryPoints} help="Relative estimate of effort. Common scales: 1, 2, 3, 5, 8, 13." />
                <MetricCard label="Completed Pts" value={metrics.completedStoryPoints} />
                <MetricCard label="Completion" value={`${Math.round(metrics.completionRate)}%`} help="Percentage of committed stories completed in the sprint." />
                <MetricCard label="Velocity" value={metrics.velocity} help="Story points completed per sprint. Higher is better, but consistency matters more." />
            </div>

            {/* Charts */}
            <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
                {storiesByStatusData.length > 0 && (
                    <div className="rounded-lg border border-border bg-card p-4">
                        <h3 className="mb-3 text-sm font-medium text-card-foreground">Stories by Status</h3>
                        <ResponsiveContainer width="100%" height={200}>
                            <BarChart data={storiesByStatusData}>
                                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                                <XAxis dataKey="name" tick={{ fontSize: 11 }} className="fill-muted-foreground" />
                                <YAxis tick={{ fontSize: 11 }} className="fill-muted-foreground" />
                                <Tooltip />
                                <Bar dataKey="count" fill="hsl(var(--primary))" radius={[4, 4, 0, 0]} />
                            </BarChart>
                        </ResponsiveContainer>
                    </div>
                )}

                {tasksByDeptData.length > 0 && (
                    <div className="rounded-lg border border-border bg-card p-4">
                        <h3 className="mb-3 text-sm font-medium text-card-foreground">Tasks by Department</h3>
                        <ResponsiveContainer width="100%" height={200}>
                            <BarChart data={tasksByDeptData}>
                                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                                <XAxis dataKey="name" tick={{ fontSize: 11 }} className="fill-muted-foreground" />
                                <YAxis tick={{ fontSize: 11 }} className="fill-muted-foreground" />
                                <Tooltip />
                                <Bar dataKey="count" fill="hsl(var(--chart-2, 220 70% 50%))" radius={[4, 4, 0, 0]} />
                            </BarChart>
                        </ResponsiveContainer>
                    </div>
                )}
            </div>
        </div>
    );
}

function MetricCard({ label, value, help }: { label: string; value: string | number; help?: string }) {
    return (
        <div className="rounded-lg border border-border bg-card p-3">
            <p className="text-xs text-muted-foreground flex items-center gap-1">{label} {help && <HelpTooltip text={help} />}</p>
            <p className="mt-0.5 text-xl font-semibold text-card-foreground">{value}</p>
        </div>
    );
}
