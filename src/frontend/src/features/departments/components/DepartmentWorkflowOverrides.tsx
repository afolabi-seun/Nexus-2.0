import { useState, useEffect, useCallback } from 'react';
import { workApi } from '@/api/workApi';

interface Props {
    canEdit: boolean;
}

interface WorkflowDef {
    storyTransitions: Record<string, string[]>;
    taskTransitions: Record<string, string[]>;
}

export function DepartmentWorkflowOverrides({ canEdit }: Props) {
    const [workflow, setWorkflow] = useState<WorkflowDef | null>(null);
    const [loading, setLoading] = useState(true);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const data = await workApi.getWorkflows() as WorkflowDef;
            setWorkflow(data);
        } catch {
            // non-critical
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { load(); }, [load]);

    if (loading || !workflow) return null;

    const storyStatuses = Object.keys(workflow.storyTransitions);
    const taskStatuses = Object.keys(workflow.taskTransitions);

    return (
        <section className="space-y-3">
            <h2 className="text-lg font-medium text-foreground">Workflow Transitions</h2>
            <p className="text-xs text-muted-foreground">
                Current workflow transitions for this department. {canEdit ? 'Contact your admin to customize.' : ''}
            </p>
            <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-1">
                    <h3 className="text-sm font-semibold text-foreground">Story ({storyStatuses.length} states)</h3>
                    <div className="rounded-md border border-border p-3 text-xs text-muted-foreground space-y-1">
                        {storyStatuses.map((s) => (
                            <div key={s} className="flex items-center gap-1">
                                <span className="font-medium text-foreground">{s}</span>
                                <span>→</span>
                                <span>{workflow.storyTransitions[s].join(', ') || 'Terminal'}</span>
                            </div>
                        ))}
                    </div>
                </div>
                <div className="space-y-1">
                    <h3 className="text-sm font-semibold text-foreground">Task ({taskStatuses.length} states)</h3>
                    <div className="rounded-md border border-border p-3 text-xs text-muted-foreground space-y-1">
                        {taskStatuses.map((s) => (
                            <div key={s} className="flex items-center gap-1">
                                <span className="font-medium text-foreground">{s}</span>
                                <span>→</span>
                                <span>{workflow.taskTransitions[s].join(', ') || 'Terminal'}</span>
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </section>
    );
}
