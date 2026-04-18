import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { timeTrackingApi } from '@/api/timeTrackingApi';
import { useAuthStore } from '@/stores/authStore';
import { ArrowRight } from 'lucide-react';

function getWeekStart(): string {
    const now = new Date();
    const day = now.getDay();
    const diff = now.getDate() - day + (day === 0 ? -6 : 1);
    const monday = new Date(now.setDate(diff));
    return monday.toISOString().slice(0, 10);
}

export function MyTimeWidget() {
    const navigate = useNavigate();
    const userId = useAuthStore((s) => s.user?.userId);
    const [hoursThisWeek, setHoursThisWeek] = useState(0);
    const [entryCount, setEntryCount] = useState(0);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (!userId) { setLoading(false); return; }
        (async () => {
            try {
                const res = await timeTrackingApi.listTimeEntries({
                    memberId: userId,
                    dateFrom: getWeekStart(),
                    pageSize: 100,
                });
                setHoursThisWeek(res.data.reduce((s, e) => s + e.hours, 0));
                setEntryCount(res.totalCount);
            } catch {
                // non-critical
            } finally {
                setLoading(false);
            }
        })();
    }, [userId]);

    if (loading) return <p className="text-sm text-muted-foreground">Loading…</p>;

    return (
        <div className="flex items-center justify-between">
            <div>
                <p className="text-2xl font-semibold text-foreground">{hoursThisWeek.toFixed(1)}h</p>
                <p className="text-xs text-muted-foreground">logged this week ({entryCount} {entryCount === 1 ? 'entry' : 'entries'})</p>
            </div>
            <button
                onClick={() => navigate('/time-tracking')}
                className="flex items-center gap-1 text-sm text-primary hover:underline"
            >
                Log time <ArrowRight size={14} />
            </button>
        </div>
    );
}
