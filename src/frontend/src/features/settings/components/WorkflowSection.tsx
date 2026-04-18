import { useState, useEffect, useCallback } from 'react';
import { workApi } from '@/api/workApi';

interface WorkflowDefinition {
    storyTransitions: Record<string, string[]>;
    taskTransitions: Record<string, string[]>;
}

export function WorkflowSection() {
    const [workflow, setWorkflow] = useState<WorkflowDefinition | null>(null);
    const [loading, setLoading] = useState(true);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const data = await workApi.getWorkflows() as WorkflowDefinition;
            setWorkflow(data);
        } catch {
            // non-critical
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { load(); }, [load]);

    if (loading) return <p className="text-sm text-muted-foreground">Loading workflows…</p>;
    if (!workflow) return null;

    return (
        <section className="space-y-4 rounded-md border border-border p-4">
            <h2 className="text-lg font-medium text-foreground">Workflow Transitions</h2>
            <p className="text-xs text-muted-foreground">
                Defines which status transitions are allowed for stories and tasks. Organization-level overrides replace the system defaults.
            </p>

            <div className="grid gap-4 md:grid-cols-2">
                <TransitionTable title="Story Transitions" transitions={workflow.storyTransitions} />
                <TransitionTable title="Task Transitions" transitions={workflow.taskTransitions} />
            </div>
        </section>
    );
}

function TransitionTable({ title, transitions }: { title: string; transitions: Record<string, string[]> }) {
    return (
        <div className="space-y-2">
            <h3 className="text-sm font-semibold text-foreground">{title}</h3>
            <div className="rounded-md border border-border overflow-hidden">
                <table className="w-full text-sm">
                    <thead>
                        <tr className="border-b border-border bg-muted/50">
                            <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">From</th>
                            <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Allowed Transitions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {Object.entries(transitions).map(([from, to]) => (
                            <tr key={from} className="border-b border-border last:border-0">
                                <td className="px-3 py-2 font-medium text-foreground">{from}</td>
                                <td className="px-3 py-2 text-muted-foreground">
                                    {to.length > 0 ? (
                                        <div className="flex flex-wrap gap-1">
                                            {to.map((s) => (
                                                <span key={s} className="rounded bg-muted px-1.5 py-0.5 text-xs">{s}</span>
                                            ))}
                                        </div>
                                    ) : (
                                        <span className="text-xs italic">Terminal state</span>
                                    )}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
}
