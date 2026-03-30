import { useState, useEffect } from 'react';
import { Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Line, ComposedChart } from 'recharts';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import type { ReportFilters, VelocityChartData } from '@/types/work';

interface VelocityChartProps {
    filters: ReportFilters;
}

export function VelocityChart({ filters }: VelocityChartProps) {
    const { addToast } = useToast();
    const [data, setData] = useState<VelocityChartData[]>([]);
    const [loading, setLoading] = useState(true);
    const [sprintCount, setSprintCount] = useState(10);

    useEffect(() => {
        setLoading(true);
        workApi.getVelocityReport({ ...filters })
            .then((d) => setData(d.slice(-sprintCount)))
            .catch(() => addToast('error', 'Failed to load velocity data'))
            .finally(() => setLoading(false));
    }, [filters, sprintCount, addToast]);

    const avgVelocity = data.length > 0 ? data.reduce((sum, d) => sum + d.velocity, 0) / data.length : 0;
    const chartData = data.map((d) => ({ ...d, average: Math.round(avgVelocity) }));

    if (loading) return <p className="text-sm text-muted-foreground">Loading...</p>;

    return (
        <div className="space-y-3">
            <div className="flex items-center justify-between">
                <h3 className="text-sm font-medium text-foreground">Sprint Velocity</h3>
                <select value={sprintCount} onChange={(e) => setSprintCount(Number(e.target.value))} className="h-8 rounded-md border border-input bg-background px-2 text-xs text-foreground">
                    {[5, 10, 15, 20].map((n) => <option key={n} value={n}>Last {n} sprints</option>)}
                </select>
            </div>
            {data.length === 0 ? (
                <p className="text-sm text-muted-foreground">No velocity data available</p>
            ) : (
                <ResponsiveContainer width="100%" height={300}>
                    <ComposedChart data={chartData}>
                        <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                        <XAxis dataKey="sprintName" tick={{ fontSize: 12 }} className="text-muted-foreground" />
                        <YAxis tick={{ fontSize: 12 }} className="text-muted-foreground" />
                        <Tooltip />
                        <Bar dataKey="velocity" fill="hsl(var(--primary))" radius={[4, 4, 0, 0]} name="Velocity" />
                        <Line type="monotone" dataKey="average" stroke="hsl(var(--destructive))" strokeDasharray="5 5" name="Average" dot={false} />
                    </ComposedChart>
                </ResponsiveContainer>
            )}
        </div>
    );
}
