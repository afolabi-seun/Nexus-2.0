import { useState, useEffect, useCallback } from 'react';
import { timeTrackingApi } from '@/api/timeTrackingApi';
import { Badge } from '@/components/common/Badge';
import { Clock } from 'lucide-react';
import type { TimeEntryResponse } from '@/types/timeTracking';

interface Props {
    storyId: string;
}

export function StoryTimeEntries({ storyId }: Props) {
    const [entries, setEntries] = useState<TimeEntryResponse[]>([]);
    const [loading, setLoading] = useState(true);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const res = await timeTrackingApi.listTimeEntries({ storyId, pageSize: 50 });
            setEntries(res.data);
        } catch {
            // non-critical
        } finally {
            setLoading(false);
        }
    }, [storyId]);

    useEffect(() => { load(); }, [load]);

    if (loading) {
        return <p className="text-sm text-muted-foreground">Loading time entries…</p>;
    }

    const totalHours = entries.reduce((sum, e) => sum + e.hours, 0);
    const billableHours = entries.filter((e) => e.billable).reduce((sum, e) => sum + e.hours, 0);

    return (
        <div className="space-y-2">
            {entries.length === 0 ? (
                <p className="text-sm text-muted-foreground">No time logged yet.</p>
            ) : (
                <>
                    <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        <span className="font-medium text-foreground">{totalHours.toFixed(1)}h total</span>
                        <span>{billableHours.toFixed(1)}h billable</span>
                        <span>{entries.length} {entries.length === 1 ? 'entry' : 'entries'}</span>
                    </div>
                    <div className="space-y-1.5">
                        {entries.map((entry) => (
                            <div key={entry.timeEntryId} className="flex items-center justify-between rounded-md border border-border px-3 py-2">
                                <div className="flex items-center gap-2 min-w-0">
                                    <Clock size={14} className="shrink-0 text-muted-foreground" />
                                    <span className="text-sm text-foreground truncate">{entry.memberName}</span>
                                    <Badge variant="status" value={entry.status} />
                                </div>
                                <div className="flex items-center gap-3 shrink-0 text-sm text-muted-foreground">
                                    <span className="font-medium text-foreground">{entry.hours}h</span>
                                    <span>{new Date(entry.date).toLocaleDateString()}</span>
                                    {entry.billable && <span className="text-xs text-green-600">Billable</span>}
                                </div>
                            </div>
                        ))}
                    </div>
                </>
            )}
        </div>
    );
}
