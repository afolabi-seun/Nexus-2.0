import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { timeTrackingApi } from '@/api/timeTrackingApi';
import { Clock, ArrowRight } from 'lucide-react';

export function PendingApprovalsWidget() {
    const navigate = useNavigate();
    const [count, setCount] = useState(0);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        (async () => {
            try {
                const res = await timeTrackingApi.listTimeEntries({ status: 'Pending', pageSize: 1 });
                setCount(res.totalCount);
            } catch {
                // non-critical
            } finally {
                setLoading(false);
            }
        })();
    }, []);

    if (loading) return <p className="text-sm text-muted-foreground">Loading…</p>;

    return (
        <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
                <div className={`flex h-10 w-10 items-center justify-center rounded-full ${count > 0 ? 'bg-yellow-100 text-yellow-600' : 'bg-muted text-muted-foreground'}`}>
                    <Clock size={20} />
                </div>
                <div>
                    <p className="text-2xl font-semibold text-foreground">{count}</p>
                    <p className="text-xs text-muted-foreground">time {count === 1 ? 'entry' : 'entries'} pending approval</p>
                </div>
            </div>
            {count > 0 && (
                <button
                    onClick={() => navigate('/time-tracking')}
                    className="flex items-center gap-1 text-sm text-primary hover:underline"
                >
                    Review <ArrowRight size={14} />
                </button>
            )}
        </div>
    );
}
