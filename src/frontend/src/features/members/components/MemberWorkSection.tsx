import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { timeTrackingApi } from '@/api/timeTrackingApi';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import { Clock, BookOpen } from 'lucide-react';
import type { TimeEntryResponse } from '@/types/timeTracking';
import type { StoryListItem } from '@/types/work';

interface Props {
    memberId: string;
}

export function MemberWorkSection({ memberId }: Props) {
    const navigate = useNavigate();
    const [timeEntries, setTimeEntries] = useState<TimeEntryResponse[]>([]);
    const [totalHours, setTotalHours] = useState(0);
    const [stories, setStories] = useState<StoryListItem[]>([]);
    const [loading, setLoading] = useState(true);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const [timeData, storyData] = await Promise.all([
                timeTrackingApi.listTimeEntries({ memberId, pageSize: 10 }).catch(() => ({ data: [], totalCount: 0 })),
                workApi.getStories({ assigneeId: memberId, page: 1, pageSize: 10 }).catch(() => ({ data: [], totalCount: 0 })),
            ]);
            setTimeEntries(timeData.data);
            setTotalHours(timeData.data.reduce((s, e) => s + e.hours, 0));
            setStories(storyData.data);
        } finally {
            setLoading(false);
        }
    }, [memberId]);

    useEffect(() => { load(); }, [load]);

    if (loading) return null;

    return (
        <>
            {/* Time Logged */}
            <section className="space-y-2">
                <h2 className="text-lg font-medium text-foreground flex items-center gap-2">
                    <Clock size={18} /> Recent Time Logged
                </h2>
                {timeEntries.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No time entries logged recently.</p>
                ) : (
                    <>
                        <p className="text-sm text-muted-foreground">{totalHours.toFixed(1)}h logged in recent entries</p>
                        <div className="space-y-1.5">
                            {timeEntries.map((entry) => (
                                <div key={entry.timeEntryId} className="flex items-center justify-between rounded-md border border-border px-3 py-2">
                                    <div className="flex items-center gap-2 min-w-0">
                                        <span className="text-sm text-foreground truncate">{entry.storyTitle || entry.storyKey}</span>
                                        <Badge variant="status" value={entry.status} />
                                    </div>
                                    <div className="flex items-center gap-3 shrink-0 text-sm text-muted-foreground">
                                        <span className="font-medium text-foreground">{entry.hours}h</span>
                                        <span>{new Date(entry.date).toLocaleDateString()}</span>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </>
                )}
            </section>

            {/* Assigned Stories */}
            <section className="space-y-2">
                <h2 className="text-lg font-medium text-foreground flex items-center gap-2">
                    <BookOpen size={18} /> Assigned Stories
                </h2>
                {stories.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No stories currently assigned.</p>
                ) : (
                    <div className="space-y-1.5">
                        {stories.map((story) => (
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
                                    <Badge variant="status" value={story.status} />
                                    <Badge variant="priority" value={story.priority} />
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </section>
        </>
    );
}
