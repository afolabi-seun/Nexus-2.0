import { useState, useEffect } from 'react';
import { useDependencyStore } from '@/stores/dependencyStore';

interface DependencyGraphProps {
    projectId: string;
}

export function DependencyGraph({ projectId }: DependencyGraphProps) {
    const [sprintFilter, setSprintFilter] = useState('');
    const { analysis, loading, error, fetchDependencies } = useDependencyStore();

    useEffect(() => {
        fetchDependencies(projectId, sprintFilter || undefined);
    }, [projectId, sprintFilter, fetchDependencies]);

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-foreground">Dependencies</h3>
                <input
                    type="text"
                    placeholder="Sprint ID (optional)"
                    value={sprintFilter}
                    onChange={(e) => setSprintFilter(e.target.value)}
                    className="rounded-md border border-input bg-background px-2 py-1 text-sm"
                />
            </div>

            {loading && <p className="text-sm text-muted-foreground">Loading…</p>}
            {error && <p className="text-sm text-destructive">{error}</p>}

            {!loading && !error && analysis && (
                <>
                    <div className="grid grid-cols-3 gap-3">
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Total Dependencies</p>
                            <p className="mt-1 text-lg font-semibold text-foreground">
                                {analysis.totalDependencies}
                            </p>
                        </div>
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Blocked Stories</p>
                            <p className="mt-1 text-lg font-semibold text-foreground">
                                {analysis.blockedStories.length}
                            </p>
                        </div>
                        <div className="rounded-lg border border-border bg-card p-3">
                            <p className="text-xs text-muted-foreground">Circular Deps</p>
                            <p className={`mt-1 text-lg font-semibold ${analysis.circularDependencies.length > 0 ? 'text-destructive' : 'text-foreground'}`}>
                                {analysis.circularDependencies.length}
                            </p>
                        </div>
                    </div>

                    {analysis.circularDependencies.length > 0 && (
                        <div>
                            <h4 className="mb-2 text-xs font-medium text-destructive">
                                Circular Dependencies
                            </h4>
                            <div className="space-y-1">
                                {analysis.circularDependencies.map((cycle, i) => (
                                    <div
                                        key={i}
                                        className="rounded border border-destructive/30 bg-destructive/5 px-3 py-1.5 text-xs text-destructive"
                                    >
                                        {cycle.join(' ↔ ')}
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}

                    {analysis.blockingChains.length > 0 && (
                        <div>
                            <h4 className="mb-2 text-xs font-medium text-muted-foreground">
                                Blocking Chains
                            </h4>
                            <div className="space-y-2">
                                {analysis.blockingChains.map((chain, i) => (
                                    <div
                                        key={i}
                                        className={`rounded-lg border p-3 ${chain.criticalPath
                                                ? 'border-yellow-500/50 bg-yellow-50/50'
                                                : 'border-border bg-card'
                                            }`}
                                    >
                                        <div className="mb-1 flex items-center gap-2 text-xs text-muted-foreground">
                                            <span>Chain length: {chain.chainLength}</span>
                                            {chain.criticalPath && (
                                                <span className="rounded bg-yellow-100 px-1.5 py-0.5 text-yellow-800">
                                                    Critical Path
                                                </span>
                                            )}
                                        </div>
                                        <div className="flex flex-wrap items-center gap-1 text-xs">
                                            {chain.stories.map((s, j) => (
                                                <span key={s.storyId} className="flex items-center gap-1">
                                                    <span className="rounded bg-muted px-1.5 py-0.5 font-mono">
                                                        {s.storyKey}
                                                    </span>
                                                    <span className="text-muted-foreground">{s.title}</span>
                                                    <span className="text-muted-foreground/60">
                                                        ({s.status})
                                                    </span>
                                                    {j < chain.stories.length - 1 && (
                                                        <span className="text-muted-foreground">→</span>
                                                    )}
                                                </span>
                                            ))}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}

                    {analysis.blockedStories.length > 0 && (
                        <div>
                            <h4 className="mb-2 text-xs font-medium text-muted-foreground">
                                Blocked Stories
                            </h4>
                            <table className="w-full text-sm">
                                <thead>
                                    <tr className="border-b border-border text-left text-xs text-muted-foreground">
                                        <th className="pb-2 pr-4">Key</th>
                                        <th className="pb-2 pr-4">Title</th>
                                        <th className="pb-2 pr-4">Status</th>
                                        <th className="pb-2 text-right">Blocked By</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {analysis.blockedStories.map((s) => (
                                        <tr key={s.storyId} className="border-b border-border/50">
                                            <td className="py-1.5 pr-4 font-mono text-xs">{s.storyKey}</td>
                                            <td className="py-1.5 pr-4">{s.title}</td>
                                            <td className="py-1.5 pr-4 text-muted-foreground">{s.status}</td>
                                            <td className="py-1.5 text-right text-muted-foreground">
                                                {s.blockedByStoryIds.length}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}
