import { useState, useEffect } from 'react';
import {
    DndContext,
    DragOverlay,
    closestCenter,
    PointerSensor,
    useSensor,
    useSensors,
    type DragStartEvent,
    type DragEndEvent,
} from '@dnd-kit/core';
import { useDroppable } from '@dnd-kit/core';
import { useDraggable } from '@dnd-kit/core';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import { useToast } from '@/components/common/Toast';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { SprintDetail, StoryListItem } from '@/types/work';
import { GripVertical } from 'lucide-react';

interface SprintPlanningViewProps {
    sprint: SprintDetail;
    onUpdated: () => void;
}

export function SprintPlanningView({ sprint, onUpdated }: SprintPlanningViewProps) {
    const { addToast } = useToast();
    const [backlogStories, setBacklogStories] = useState<StoryListItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [activeStory, setActiveStory] = useState<StoryListItem | null>(null);

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 5 } })
    );

    useEffect(() => {
        setLoading(true);
        workApi
            .getStories({ projectId: sprint.projectId, page: 1, pageSize: 200 })
            .then((res) => {
                const sprintStoryIds = new Set(sprint.stories.map((s) => s.storyId));
                setBacklogStories(res.data.filter((s) => !sprintStoryIds.has(s.storyId) && !s.sprintName));
            })
            .catch(() => addToast('error', 'Failed to load backlog'))
            .finally(() => setLoading(false));
    }, [sprint, addToast]);

    const handleDragStart = (event: DragStartEvent) => {
        const id = String(event.active.id);
        const allStories = [...backlogStories, ...sprint.stories];
        const story = allStories.find((s) => s.storyId === id);
        setActiveStory(story ?? null);
    };

    const handleDragEnd = async (event: DragEndEvent) => {
        setActiveStory(null);
        const { active, over } = event;
        if (!over) return;

        const storyId = String(active.id);
        const target = String(over.id);

        if (target === 'sprint-panel') {
            // Moving from backlog to sprint
            const story = backlogStories.find((s) => s.storyId === storyId);
            if (!story) return;

            // Optimistic update
            setBacklogStories((prev) => prev.filter((s) => s.storyId !== storyId));

            try {
                await workApi.addStoryToSprint(sprint.sprintId, { storyId });
                addToast('success', `Added "${story.storyKey}" to sprint`);
                onUpdated();
            } catch (err) {
                // Revert
                setBacklogStories((prev) => [...prev, story]);
                if (err instanceof ApiError) {
                    addToast('error', mapErrorCode(err.errorCode));
                } else {
                    addToast('error', 'Failed to add story to sprint');
                }
            }
        } else if (target === 'backlog-panel') {
            // Moving from sprint to backlog
            const story = sprint.stories.find((s) => s.storyId === storyId);
            if (!story) return;

            try {
                await workApi.removeStoryFromSprint(sprint.sprintId, storyId);
                addToast('success', `Removed "${story.storyKey}" from sprint`);
                onUpdated();
            } catch (err) {
                if (err instanceof ApiError) {
                    addToast('error', mapErrorCode(err.errorCode));
                } else {
                    addToast('error', 'Failed to remove story from sprint');
                }
            }
        }
    };

    const totalPoints = sprint.stories.reduce((sum, s) => sum + (s.storyPoints ?? 0), 0);

    return (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
            <div className="grid grid-cols-2 gap-4">
                {/* Backlog panel */}
                <DroppablePanel id="backlog-panel" title="Backlog" count={backlogStories.length}>
                    {loading ? (
                        <p className="p-4 text-sm text-muted-foreground">Loading...</p>
                    ) : backlogStories.length === 0 ? (
                        <p className="p-4 text-sm text-muted-foreground">No stories in backlog</p>
                    ) : (
                        <div className="space-y-1.5 p-2">
                            {backlogStories.map((story) => (
                                <DraggableStoryCard key={story.storyId} story={story} />
                            ))}
                        </div>
                    )}
                </DroppablePanel>

                {/* Sprint panel */}
                <DroppablePanel
                    id="sprint-panel"
                    title={`Sprint: ${sprint.name}`}
                    count={sprint.stories.length}
                    subtitle={`${totalPoints} pts`}
                >
                    {sprint.stories.length === 0 ? (
                        <p className="p-4 text-sm text-muted-foreground">Drag stories here to add them to the sprint</p>
                    ) : (
                        <div className="space-y-1.5 p-2">
                            {sprint.stories.map((story) => (
                                <DraggableStoryCard key={story.storyId} story={story} />
                            ))}
                        </div>
                    )}
                </DroppablePanel>
            </div>

            <DragOverlay>
                {activeStory && <StoryCardContent story={activeStory} isDragging />}
            </DragOverlay>
        </DndContext>
    );
}

function DroppablePanel({
    id,
    title,
    count,
    subtitle,
    children,
}: {
    id: string;
    title: string;
    count: number;
    subtitle?: string;
    children: React.ReactNode;
}) {
    const { setNodeRef, isOver } = useDroppable({ id });

    return (
        <div
            ref={setNodeRef}
            className={`rounded-lg border-2 ${isOver ? 'border-primary bg-primary/5' : 'border-border'} min-h-[300px] transition-colors`}
        >
            <div className="flex items-center justify-between border-b border-border px-3 py-2">
                <h3 className="text-sm font-semibold text-foreground">{title}</h3>
                <div className="flex items-center gap-2">
                    {subtitle && <span className="text-xs text-muted-foreground">{subtitle}</span>}
                    <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">{count}</span>
                </div>
            </div>
            {children}
        </div>
    );
}

function DraggableStoryCard({ story }: { story: StoryListItem }) {
    const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
        id: story.storyId,
    });

    const style = transform
        ? { transform: `translate(${transform.x}px, ${transform.y}px)` }
        : undefined;

    return (
        <div
            ref={setNodeRef}
            style={style}
            {...listeners}
            {...attributes}
            className={`${isDragging ? 'opacity-50' : ''}`}
        >
            <StoryCardContent story={story} />
        </div>
    );
}

function StoryCardContent({ story, isDragging }: { story: StoryListItem; isDragging?: boolean }) {
    return (
        <div className={`flex items-center gap-2 rounded-md border border-border bg-card p-2.5 ${isDragging ? 'shadow-lg ring-2 ring-primary' : 'hover:bg-accent/50'}`}>
            <GripVertical size={14} className="shrink-0 text-muted-foreground" />
            <div className="min-w-0 flex-1">
                <div className="flex items-center gap-1.5">
                    <span className="text-xs font-medium text-muted-foreground">{story.storyKey}</span>
                    <span className="text-sm text-foreground truncate">{story.title}</span>
                </div>
                <div className="mt-1 flex items-center gap-1.5">
                    <Badge variant="priority" value={story.priority} />
                    {story.storyPoints != null && (
                        <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">
                            {story.storyPoints} pts
                        </span>
                    )}
                </div>
            </div>
        </div>
    );
}
