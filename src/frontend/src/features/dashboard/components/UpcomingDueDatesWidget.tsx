import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { useAuthStore } from '@/stores/authStore';
import { Badge } from '@/components/common/Badge';
import type { StoryListItem } from '@/types/work';

function daysUntil(dateStr: string): number {
    const due = new Date(dateStr);
    const now = new Date();
    return Math.ceil((due.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
}

function dueLabel(days: number): { text: string; className: string } {
    if (days < 0) return { text: `${Math.abs(days)}d overdue`, className: 'text-destructive' };
    if (days === 0) return { text: 'Due today', className: 'text-yellow-600' };
    if (days <= 2) return { text: `Due in ${days}d`, className: 'text-yellow-600' };
    return { text: `Due in ${days}d`, className: 'text-muted-foreground' };
}

export function UpcomingDueDatesWidget() {
    const navigate = useNavigate();
    const userId = useAuthStore((s) => s.user?.userId);
    const [stories, setStories] = useState<StoryListItem[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (!userId) { setLoading(false); return; }
        (async () => {
            try {
                const res = await workApi.getStories({
                    assigneeId: userId,
                    page: 1,
                    pageSize: 50,
                });
                const withDue = res.data
                    .filter((s) => s.dueDate && s.status !== 'Done' && s.status !== 'Closed')
                    .sort((a, b) => new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime())
                    .slice(0, 5);
                setStories(withDue);
            } catch {
                // non-critical
            } finally {
                setLoading(false);
            }
        })();
    }, [userId]);

    if (loading) return <p className="text-sm text-muted-foreground">Loading…</p>;
    if (stories.length === 0) return <p className="text-sm text-muted-foreground">No upcoming due dates.</p>;

    return (
        <div className="space-y-1.5">
            {stories.map((story) => {
                const days = daysUntil(story.dueDate!);
                const due = dueLabel(days);
                return (
                    <div
                        key={story.storyId}
                        onClick={() => navigate(`/stories/${story.storyId}`)}
                        className="flex items-center justify-between rounded-md border border-border px-3 py-2 cursor-pointer hover:bg-accent"
                    >
                        <div className="flex items-center gap-2 min-w-0">
                            <span className="text-xs font-medium text-muted-foreground">{story.storyKey}</span>
                            <span className="text-sm text-foreground truncate">{story.title}</span>
                        </div>
                        <div className="flex items-center gap-2 shrink-0">
                            <Badge variant="priority" value={story.priority} />
                            <span className={`text-xs font-medium ${due.className}`}>{due.text}</span>
                        </div>
                    </div>
                );
            })}
        </div>
    );
}
