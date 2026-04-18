import { useState, useEffect, useCallback } from 'react';
import { analyticsApi } from '@/api/analyticsApi';
import type { SnapshotStatusResponse } from '@/types/analytics';

export function SnapshotStatusSection() {
    const [status, setStatus] = useState<SnapshotStatusResponse | null>(null);
    const [loading, setLoading] = useState(true);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const data = await analyticsApi.getSnapshotStatus();
            setStatus(data);
        } catch {
            // non-critical
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { load(); }, [load]);

    if (loading || !status) return null;

    return (
        <section className="space-y-4 rounded-md border border-border p-4">
            <h2 className="text-lg font-medium text-foreground">Analytics Snapshots</h2>
            <p className="text-xs text-muted-foreground">
                Background service generates health, velocity, and cost snapshots for all projects.
            </p>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                <div className="rounded-lg border border-border bg-card p-3">
                    <p className="text-xs text-muted-foreground">Last Run</p>
                    <p className="mt-1 text-sm font-medium text-foreground">
                        {status.lastRunTime ? new Date(status.lastRunTime).toLocaleString() : 'Never'}
                    </p>
                </div>
                <div className="rounded-lg border border-border bg-card p-3">
                    <p className="text-xs text-muted-foreground">Projects Processed</p>
                    <p className="mt-1 text-sm font-medium text-foreground">{status.projectsProcessed}</p>
                </div>
                <div className="rounded-lg border border-border bg-card p-3">
                    <p className="text-xs text-muted-foreground">Errors</p>
                    <p className={`mt-1 text-sm font-medium ${status.errorsEncountered > 0 ? 'text-destructive' : 'text-foreground'}`}>
                        {status.errorsEncountered}
                    </p>
                </div>
                <div className="rounded-lg border border-border bg-card p-3">
                    <p className="text-xs text-muted-foreground">Next Run</p>
                    <p className="mt-1 text-sm font-medium text-foreground">
                        {status.nextScheduledRun ? new Date(status.nextScheduledRun).toLocaleString() : '—'}
                    </p>
                </div>
            </div>
        </section>
    );
}
