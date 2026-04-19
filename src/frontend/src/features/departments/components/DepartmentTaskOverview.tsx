import { useState, useEffect, useCallback } from 'react';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import type { DepartmentBoard } from '@/types/work';

interface Props {
    departmentId: string;
}

export function DepartmentTaskOverview({ departmentId }: Props) {
    const [board, setBoard] = useState<DepartmentBoard | null>(null);
    const [loading, setLoading] = useState(true);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const data = await workApi.getDepartmentBoard({ departmentId });
            setBoard(data);
        } catch {
            // non-critical
        } finally {
            setLoading(false);
        }
    }, [departmentId]);

    useEffect(() => { load(); }, [load]);

    if (loading || !board) return null;

    const dept = board.departments?.find((d) => d.departmentName === departmentId || d.taskCount > 0);
    if (!dept || dept.taskCount === 0) return null;

    return (
        <section className="space-y-2">
            <h2 className="text-lg font-medium text-foreground">Tasks by Status</h2>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                {Object.entries(dept.tasksByStatus).map(([status, count]) => (
                    <div key={status} className="rounded-lg border border-border bg-card p-3">
                        <div className="flex items-center gap-1.5">
                            <Badge variant="status" value={status} />
                        </div>
                        <p className="mt-1 text-lg font-semibold text-foreground">{count}</p>
                    </div>
                ))}
            </div>
        </section>
    );
}
