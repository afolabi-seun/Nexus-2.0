import { useEffect, useState } from 'react';
import { workApi } from '@/api/workApi';
import { useAuthStore } from '@/stores/authStore';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { Badge } from '@/components/common/Badge';
import type { StoryListItem } from '@/types/work';
import { useNavigate } from 'react-router-dom';

export function MyTasksWidget() {
    const [stories, setStories] = useState<StoryListItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const userId = useAuthStore((s) => s.user?.userId);
    const navigate = useNavigate();

    useEffect(() => {
        if (!userId) {
            setLoading(false);
            return;
        }
        let cancelled = false;
        async function fetch() {
            try {
                const data = await workApi.getStories({
                    assigneeId: userId,
                    status: ['InProgress', 'InReview'],
                    pageSize: 10,
                });
                if (!cancelled) setStories(data.data);
            } catch (err) {
                if (!cancelled) setError(err instanceof Error ? err.message : 'Failed to load tasks');
            } finally {
                if (!cancelled) setLoading(false);
            }
        }
        fetch();
        return () => { cancelled = true; };
    }, [userId]);

    if (loading) return <SkeletonLoader variant="table" rows={4} columns={3} />;

    if (error) {
        return (
            <div className="flex items-center justify-center rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-sm text-destructive">
                {error}
            </div>
        );
    }

    if (stories.length === 0) {
        return (
            <div className="flex items-center justify-center p-6 text-sm text-muted-foreground">
                No tasks assigned to you
            </div>
        );
    }

    return (
        <ul className="divide-y divide-border">
            {stories.map((story) => (
                <li
                    key={story.storyId}
                    className="flex cursor-pointer items-center justify-between gap-3 px-1 py-2.5 hover:bg-muted/50"
                    onClick={() => navigate(`/stories/${story.storyId}`)}
                >
                    <div className="min-w-0 flex-1">
                        <span className="mr-2 text-xs font-medium text-muted-foreground">{story.storyKey}</span>
                        <span className="text-sm text-foreground truncate">{story.title}</span>
                    </div>
                    <Badge variant="status" value={story.status} />
                </li>
            ))}
        </ul>
    );
}
