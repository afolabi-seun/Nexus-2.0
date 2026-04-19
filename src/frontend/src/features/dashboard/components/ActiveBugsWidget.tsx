import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { Bug, ArrowRight } from 'lucide-react';

export function ActiveBugsWidget() {
    const navigate = useNavigate();
    const [bugCount, setBugCount] = useState(0);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        (async () => {
            try {
                const res = await workApi.getStories({ priority: ['Bug'], page: 1, pageSize: 1 });
                const openBugs = res.totalCount;
                setBugCount(openBugs);
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
                <div className={`flex h-10 w-10 items-center justify-center rounded-full ${bugCount > 0 ? 'bg-red-100 text-red-600' : 'bg-muted text-muted-foreground'}`}>
                    <Bug size={20} />
                </div>
                <div>
                    <p className="text-2xl font-semibold text-foreground">{bugCount}</p>
                    <p className="text-xs text-muted-foreground">open {bugCount === 1 ? 'bug' : 'bugs'} across projects</p>
                </div>
            </div>
            {bugCount > 0 && (
                <button
                    onClick={() => navigate('/stories?priority=Bug')}
                    className="flex items-center gap-1 text-sm text-primary hover:underline"
                >
                    View <ArrowRight size={14} />
                </button>
            )}
        </div>
    );
}
