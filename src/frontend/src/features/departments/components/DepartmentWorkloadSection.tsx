import { useState, useEffect, useCallback } from 'react';
import { workApi } from '@/api/workApi';
import type { DepartmentWorkloadData } from '@/types/work';

interface Props {
    departmentId: string;
}

export function DepartmentWorkloadSection({ departmentId }: Props) {
    const [workload, setWorkload] = useState<DepartmentWorkloadData | null>(null);
    const [loading, setLoading] = useState(true);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const data = await workApi.getDepartmentWorkloadReport({ departmentId });
            setWorkload(data.length > 0 ? data[0] : null);
        } catch {
            // non-critical
        } finally {
            setLoading(false);
        }
    }, [departmentId]);

    useEffect(() => { load(); }, [load]);

    if (loading || !workload) return null;

    return (
        <section className="space-y-2">
            <h2 className="text-lg font-medium text-foreground">Workload</h2>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                <div className="rounded-lg border border-border bg-card p-3">
                    <p className="text-xs text-muted-foreground">Total Tasks</p>
                    <p className="mt-1 text-lg font-semibold text-foreground">{workload.totalTasks}</p>
                </div>
                {Object.entries(workload.tasksByType ?? {}).map(([type, count]) => (
                    <div key={type} className="rounded-lg border border-border bg-card p-3">
                        <p className="text-xs text-muted-foreground">{type}</p>
                        <p className="mt-1 text-lg font-semibold text-foreground">{count}</p>
                    </div>
                ))}
            </div>
        </section>
    );
}
