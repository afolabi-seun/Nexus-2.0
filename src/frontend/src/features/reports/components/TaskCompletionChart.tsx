import { useState, useEffect } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import type { ReportFilters, TaskCompletionData } from '@/types/work';

interface TaskCompletionChartProps {
    filters: ReportFilters;
}

export function TaskCompletionChart({ filters }: TaskCompletionChartProps) {
    const { addToast } = useToast();
    const [data, setData] = useState<TaskCompletionData[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        setLoading(true);
        workApi.getTaskCompletionReport(filters)
            .then(setData)
            .catch(() => addToast('error', 'Failed to load completion data'))
            .finally(() => setLoading(false));
    }, [filters, addToast]);

    if (loading) return <p className="text-sm text-muted-foreground">Loading...</p>;
    if (data.length === 0) return <p className="text-sm text-muted-foreground">No completion data available</p>;

    // Transform data: each department gets completed and total counts
    const chartData = data.map((d) => {
        let completed = 0;
        let total = 0;
        Object.values(d.completionsByType).forEach((v) => {
            completed += v.completed;
            total += v.total;
        });
        return {
            department: d.departmentName,
            completed,
            remaining: total - completed,
            rate: total > 0 ? Math.round((completed / total) * 100) : 0,
        };
    });

    return (
        <div className="space-y-3">
            <h3 className="text-sm font-medium text-foreground">Task Completion Rate</h3>
            <ResponsiveContainer width="100%" height={300}>
                <BarChart data={chartData}>
                    <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                    <XAxis dataKey="department" tick={{ fontSize: 12 }} />
                    <YAxis tick={{ fontSize: 12 }} />
                    <Tooltip />
                    <Legend />
                    <Bar dataKey="completed" stackId="a" fill="#10b981" name="Completed" />
                    <Bar dataKey="remaining" stackId="a" fill="#ef4444" name="Remaining" />
                </BarChart>
            </ResponsiveContainer>
        </div>
    );
}
