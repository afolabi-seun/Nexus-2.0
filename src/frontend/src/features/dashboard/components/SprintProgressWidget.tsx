import { useEffect, useState } from 'react';
import { workApi } from '@/api/workApi';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import type { SprintDetail } from '@/types/work';
import { differenceInDays, parseISO } from 'date-fns';

export function SprintProgressWidget() {
    const [sprint, setSprint] = useState<SprintDetail | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;
        async function fetch() {
            try {
                const data = await workApi.getActiveSprint();
                if (!cancelled) setSprint(data);
            } catch (err) {
                if (!cancelled) setError(err instanceof Error ? err.message : 'Failed to load sprint progress');
            } finally {
                if (!cancelled) setLoading(false);
            }
        }
        fetch();
        return () => { cancelled = true; };
    }, []);

    if (loading) return <SkeletonLoader variant="card" />;

    if (error) {
        return (
            <div className="flex items-center justify-center rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-sm text-destructive">
                {error}
            </div>
        );
    }

    if (!sprint) {
        return (
            <div className="flex items-center justify-center p-6 text-sm text-muted-foreground">
                No active sprint
            </div>
        );
    }

    const completedPoints = sprint.completedStoryPoints ?? 0;
    const totalPoints = sprint.totalStoryPoints ?? 0;
    const progressPct = totalPoints > 0 ? Math.round((completedPoints / totalPoints) * 100) : 0;
    const remainingDays = Math.max(0, differenceInDays(parseISO(sprint.endDate), new Date()));

    return (
        <div className="space-y-4">
            <p className="text-sm font-medium text-foreground">{sprint.name}</p>

            <div className="space-y-1">
                <div className="flex justify-between text-xs text-muted-foreground">
                    <span>{completedPoints} / {totalPoints} story points</span>
                    <span>{progressPct}%</span>
                </div>
                <div className="h-2.5 w-full overflow-hidden rounded-full bg-muted">
                    <div
                        className="h-full rounded-full bg-primary transition-all"
                        style={{ width: `${progressPct}%` }}
                    />
                </div>
            </div>

            <div className="flex justify-between text-xs text-muted-foreground">
                <span>{remainingDays} day{remainingDays !== 1 ? 's' : ''} remaining</span>
                <span>{sprint.storyCount} stories</span>
            </div>
        </div>
    );
}
