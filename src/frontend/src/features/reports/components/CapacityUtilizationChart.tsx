import { useState, useEffect } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import type { ReportFilters, CapacityUtilizationData } from '@/types/work';

interface CapacityUtilizationChartProps {
    filters: ReportFilters;
}

export function CapacityUtilizationChart({ filters }: CapacityUtilizationChartProps) {
    const { addToast } = useToast();
    const [data, setData] = useState<CapacityUtilizationData[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        setLoading(true);
        workApi.getCapacityReport(filters)
            .then(setData)
            .catch(() => addToast('error', 'Failed to load capacity data'))
            .finally(() => setLoading(false));
    }, [filters, addToast]);

    if (loading) return <p className="text-sm text-muted-foreground">Loading...</p>;
    if (data.length === 0) return <p className="text-sm text-muted-foreground">No capacity data available</p>;

    // Flatten members across departments for chart
    const chartData = data.flatMap((dept) =>
        dept.members.map((m) => ({
            name: `${m.memberName} (${dept.departmentName})`,
            activeTasks: m.activeTasks,
            maxTasks: m.maxTasks,
            utilization: m.maxTasks > 0 ? Math.round((m.activeTasks / m.maxTasks) * 100) : 0,
        }))
    ).slice(0, 20); // Limit to 20 for readability

    return (
        <div className="space-y-3">
            <h3 className="text-sm font-medium text-foreground">Capacity Utilization</h3>
            <ResponsiveContainer width="100%" height={300}>
                <BarChart data={chartData} layout="vertical">
                    <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                    <XAxis type="number" tick={{ fontSize: 12 }} />
                    <YAxis dataKey="name" type="category" tick={{ fontSize: 10 }} width={150} />
                    <Tooltip />
                    <Legend />
                    <Bar dataKey="activeTasks" fill="hsl(var(--primary))" name="Active Tasks" />
                    <Bar dataKey="maxTasks" fill="hsl(var(--muted))" name="Max Tasks" />
                </BarChart>
            </ResponsiveContainer>
        </div>
    );
}
