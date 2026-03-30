import { useState, useEffect } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import type { ReportFilters, DepartmentWorkloadData } from '@/types/work';

const COLORS = ['#3b82f6', '#ef4444', '#f59e0b', '#10b981', '#8b5cf6', '#ec4899'];

interface DepartmentWorkloadChartProps {
    filters: ReportFilters;
}

export function DepartmentWorkloadChart({ filters }: DepartmentWorkloadChartProps) {
    const { addToast } = useToast();
    const [data, setData] = useState<DepartmentWorkloadData[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        setLoading(true);
        workApi.getDepartmentWorkloadReport(filters)
            .then(setData)
            .catch(() => addToast('error', 'Failed to load workload data'))
            .finally(() => setLoading(false));
    }, [filters, addToast]);

    if (loading) return <p className="text-sm text-muted-foreground">Loading...</p>;
    if (data.length === 0) return <p className="text-sm text-muted-foreground">No workload data available</p>;

    // Collect all task types across departments
    const allTypes = new Set<string>();
    data.forEach((d) => Object.keys(d.tasksByType).forEach((t) => allTypes.add(t)));
    const taskTypes = Array.from(allTypes);

    const chartData = data.map((d) => ({
        department: d.departmentName,
        ...d.tasksByType,
        total: d.totalTasks,
    }));

    return (
        <div className="space-y-3">
            <h3 className="text-sm font-medium text-foreground">Department Workload</h3>
            <ResponsiveContainer width="100%" height={300}>
                <BarChart data={chartData}>
                    <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                    <XAxis dataKey="department" tick={{ fontSize: 12 }} />
                    <YAxis tick={{ fontSize: 12 }} />
                    <Tooltip />
                    <Legend />
                    {taskTypes.map((type, i) => (
                        <Bar key={type} dataKey={type} stackId="a" fill={COLORS[i % COLORS.length]} name={type} />
                    ))}
                </BarChart>
            </ResponsiveContainer>
        </div>
    );
}
