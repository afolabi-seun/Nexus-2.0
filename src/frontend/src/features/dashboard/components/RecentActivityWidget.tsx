import { useEffect, useState } from 'react';
import { utilityApi } from '@/api/utilityApi';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import type { AuditLog } from '@/types/utility';
import { formatRelative } from '@/utils/dateFormatting';

export function RecentActivityWidget() {
    const [logs, setLogs] = useState<AuditLog[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;
        async function fetch() {
            try {
                const data = await utilityApi.getAuditLogs({
                    action: 'StoryCreated,TaskAssigned,StatusChanged',
                    pageSize: 20,
                });
                if (!cancelled) setLogs(data.data);
            } catch (err) {
                if (!cancelled) setError(err instanceof Error ? err.message : 'Failed to load activity');
            } finally {
                if (!cancelled) setLoading(false);
            }
        }
        fetch();
        return () => { cancelled = true; };
    }, []);

    if (loading) return <SkeletonLoader variant="table" rows={5} columns={2} />;

    if (error) {
        return (
            <div className="flex items-center justify-center rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-sm text-destructive">
                {error}
            </div>
        );
    }

    if (logs.length === 0) {
        return (
            <div className="flex items-center justify-center p-6 text-sm text-muted-foreground">
                No recent activity
            </div>
        );
    }

    return (
        <ul className="divide-y divide-border">
            {logs.map((log) => (
                <li key={log.auditLogId} className="px-1 py-2.5">
                    <div className="flex items-start justify-between gap-2">
                        <div className="min-w-0 flex-1">
                            <span className="text-sm font-medium text-foreground">{log.actorName}</span>
                            <span className="ml-1 text-sm text-muted-foreground">
                                {formatAction(log.action, log.entityType)}
                            </span>
                        </div>
                        <span className="shrink-0 text-xs text-muted-foreground">
                            {formatRelative(log.dateCreated)}
                        </span>
                    </div>
                    {log.details && (
                        <p className="mt-0.5 text-xs text-muted-foreground truncate">{log.details}</p>
                    )}
                </li>
            ))}
        </ul>
    );
}

function formatAction(action: string, entityType: string): string {
    const actionMap: Record<string, string> = {
        StoryCreated: 'created a story',
        TaskAssigned: `assigned a ${entityType.toLowerCase() || 'task'}`,
        StatusChanged: `changed status of a ${entityType.toLowerCase() || 'item'}`,
    };
    return actionMap[action] ?? action;
}
