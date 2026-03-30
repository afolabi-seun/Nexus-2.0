import { useState, useEffect } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import type { ReportFilters, CycleTimeData } from '@/types/work';

interface CycleTimeChartProps {
    filters: ReportFilters;
}

export function CycleTimeChart({ filters }: CycleTimeChartProps) {
    const { addToast } = useToast();
    const [data, setData] = useState<CycleTimeData[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        setLoading(true);
        workApi.getCycleTimeReport(filters)
            .then(setData)
            .catch(() => addToast('error', 'Failed to load cycle time data'))
            .finally(() => setLoading(false));
    }, [filters, addToast]);

    if (loading) return <p className="text-sm text-muted-foreground">Loading...</p>;
    if (data.length === 0) return <p className="text-sm text-muted-foreground">No cycle time data available</p>;

    return (
        <div className="space-y-3">
            <h3 className="text-sm font-medium text-foreground">Average Cycle Time (days)</h3>
            <ResponsiveContainer width="100%" height={300}>
                <LineChart data={data}>
                    <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                    <XAxis dataKey="period" tick={{ fontSize: 12 }} />
                    <YAxis tick={{ fontSize: 12 }} />
                    <Tooltip />
                    <Line type="monotone" dataKey="averageDays" stroke="hsl(var(--primary))" strokeWidth={2} dot={{ r: 4 }} name="Avg Days" />
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}
