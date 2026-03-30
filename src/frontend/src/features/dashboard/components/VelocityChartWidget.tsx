import { useEffect, useState } from 'react';
import { workApi } from '@/api/workApi';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import type { VelocityChartData } from '@/types/work';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

export function VelocityChartWidget() {
    const [data, setData] = useState<VelocityChartData[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;
        async function fetch() {
            try {
                const result = await workApi.getVelocity({ count: 10 });
                if (!cancelled) setData(result);
            } catch (err) {
                if (!cancelled) setError(err instanceof Error ? err.message : 'Failed to load velocity data');
            } finally {
                if (!cancelled) setLoading(false);
            }
        }
        fetch();
        return () => { cancelled = true; };
    }, []);

    if (loading) return <SkeletonLoader variant="card" />;

    if (error) {
        return (
            <div className="flex items-center justify-center rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-sm text-destructive">
                {error}
            </div>
        );
    }

    if (data.length === 0) {
        return (
            <div className="flex items-center justify-center p-6 text-sm text-muted-foreground">
                No velocity data available
            </div>
        );
    }

    return (
        <div className="h-64 w-full">
            <ResponsiveContainer width="100%" height="100%">
                <BarChart data={data} margin={{ top: 4, right: 8, left: 0, bottom: 4 }}>
                    <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                    <XAxis
                        dataKey="sprintName"
                        tick={{ fontSize: 11 }}
                        className="fill-muted-foreground"
                    />
                    <YAxis
                        tick={{ fontSize: 11 }}
                        className="fill-muted-foreground"
                    />
                    <Tooltip
                        contentStyle={{
                            backgroundColor: 'hsl(var(--card))',
                            border: '1px solid hsl(var(--border))',
                            borderRadius: '0.375rem',
                            fontSize: '0.75rem',
                        }}
                        labelStyle={{ color: 'hsl(var(--foreground))' }}
                    />
                    <Bar
                        dataKey="velocity"
                        fill="hsl(var(--primary))"
                        radius={[4, 4, 0, 0]}
                        name="Velocity"
                    />
                </BarChart>
            </ResponsiveContainer>
        </div>
    );
}
