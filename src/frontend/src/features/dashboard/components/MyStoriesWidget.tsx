import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { useAuthStore } from '@/stores/authStore';
import { Badge } from '@/components/common/Badge';
import type { StoryListItem } from '@/types/work';

export function MyStoriesWidget() {
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
                    pageSize: 10,
                });
                setStories(res.data.filter((s) => s.status !== 'Done' && s.status !== 'Closed'));
            } catch {
                // non-critical
            } finally {
                setLoading(false);
            }
        })();
    }, [userId]);

    if (loading) return <p className="text-sm text-muted-foreground">Loading…</p>;
    if (stories.length === 0) return <p className="text-sm text-muted-foreground">No active stories assigned to you.</p>;

    const byStatus: Record<string, number> = {};
    stories.forEach((s) => { byStatus[s.status] = (byStatus[s.status] ?? 0) + 1; });

    return (
        <div className="space-y-3">
            <div className="flex flex-wrap gap-3">
                {Object.entries(byStatus).map(([status, count]) => (
                    <div key={status} className="flex items-center gap-1.5">
                        <Badge variant="status" value={status} />
                        <span className="text-sm font-medium text-foreground">{count}</span>
                    </div>
                ))}
            </div>
            <div className="space-y-1.5">
                {stories.slice(0, 5).map((story) => (
                    <div
                        key={story.storyId}
                        onClick={() => navigate(`/stories/${story.storyId}`)}
                        className="flex items-center justify-between rounded-md border border-border px-3 py-2 cursor-pointer hover:bg-accent"
                    >
                        <div className="flex items-center gap-2 min-w-0">
                            <span className="text-xs font-medium text-muted-foreground">{story.storyKey}</span>
                            <span className="text-sm text-foreground truncate">{story.title}</span>
                        </div>
                        <Badge variant="priority" value={story.priority} />
                    </div>
                ))}
            </div>
        </div>
    );
}
