import { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { DndContext, DragOverlay } from '@dnd-kit/core';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { ListFilter } from '@/components/common/ListFilter';
import { useListFilters } from '@/hooks/useListFilters';
import { StoryStatus, Priority } from '@/types/enums';
import type { FilterConfig } from '@/types/filters';
import type { KanbanBoard, KanbanCard } from '@/types/work';
import { BoardColumn } from '../components/BoardColumn.js';
import { DraggableCard, StoryCardContent } from '../components/DraggableCard.js';
import { useBoardDragDrop } from '../hooks/useBoardDragDrop';
import { PageHeader } from '@/components/common/PageHeader';

const KANBAN_COLUMNS = [
    StoryStatus.Backlog,
    StoryStatus.Ready,
    StoryStatus.InProgress,
    StoryStatus.InReview,
    StoryStatus.QA,
    StoryStatus.Done,
    StoryStatus.Closed,
];

const filterConfigs: FilterConfig[] = [
    {
        key: 'projectId',
        label: 'Project',
        type: 'select',
        loadOptions: async () => {
            const res = await workApi.getProjects({ page: 1, pageSize: 100 });
            return res.data.map((p) => ({ value: p.projectId, label: p.name }));
        },
    },
    {
        key: 'priority',
        label: 'Priority',
        type: 'multi-select',
        options: Object.values(Priority).map((p) => ({ value: p, label: p })),
    },
];

export function KanbanBoardPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const [board, setBoard] = useState<KanbanBoard | null>(null);
    const [loading, setLoading] = useState(true);

    const { filterValues, updateFilter, clearFilters, hasActiveFilters, activeFilterCount } =
        useListFilters(filterConfigs, { syncToUrl: true });

    const apiFilters = useMemo(() => ({
        projectId: filterValues.projectId as string | undefined,
        priority: Array.isArray(filterValues.priority)
            ? filterValues.priority[0]
            : (filterValues.priority as string | undefined),
    }), [filterValues]);

    const fetchBoard = useCallback(async () => {
        setLoading(true);
        try {
            const data = await workApi.getKanbanBoard(apiFilters);
            setBoard(data);
        } catch {
            addToast('error', 'Failed to load kanban board');
        } finally {
            setLoading(false);
        }
    }, [apiFilters, addToast]);

    useEffect(() => { fetchBoard(); }, [fetchBoard]);

    // Flatten all cards for drag-drop
    const allCards: (KanbanCard & { _status: StoryStatus })[] = board
        ? board.columns.flatMap((col) => col.cards.map((c) => ({ ...c, _status: col.status })))
        : [];

    const {
        sensors,
        activeItem,
        handleDragStart,
        handleDragEnd,
        getEffectiveColumn,
    } = useBoardDragDrop({
        items: allCards,
        getItemId: (c) => c.storyId,
        getItemColumn: (c) => c._status,
        onMove: async (itemId, _from, toColumn) => {
            await workApi.updateStoryStatus(itemId, { status: toColumn as StoryStatus });
        },
        onRefresh: fetchBoard,
    });

    if (loading) return <SkeletonLoader variant="table" rows={5} columns={7} />;

    // Group cards by effective column
    const cardsByColumn: Record<string, typeof allCards> = {};
    for (const col of KANBAN_COLUMNS) {
        cardsByColumn[col] = [];
    }
    for (const card of allCards) {
        const col = getEffectiveColumn(card);
        if (cardsByColumn[col]) {
            cardsByColumn[col].push(card);
        }
    }

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <PageHeader title="Kanban Board" description="Drag stories between columns to update their status. Filter by project or sprint." dismissKey="kanban" />
            </div>

            <ListFilter
                configs={filterConfigs}
                values={filterValues}
                onUpdateFilter={updateFilter}
                onClearFilters={clearFilters}
                hasActiveFilters={hasActiveFilters}
                activeFilterCount={activeFilterCount}
                enableSavedFilters
            />

            <DndContext sensors={sensors} onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
                <div className="flex gap-3 overflow-x-auto pb-4">
                    {KANBAN_COLUMNS.map((status) => {
                        const colCards = cardsByColumn[status] ?? [];
                        const origCol = board?.columns.find((c) => c.status === status);
                        return (
                            <BoardColumn
                                key={status}
                                id={status}
                                name={status}
                                cardCount={colCards.length}
                                totalPoints={origCol?.totalPoints}
                            >
                                {colCards.map((card) => (
                                    <DraggableCard key={card.storyId} id={card.storyId}>
                                        <div onClick={() => navigate(`/stories/${card.storyId}`)}>
                                            <StoryCardContent
                                                storyKey={card.storyKey}
                                                title={card.title}
                                                priority={card.priority}
                                                storyPoints={card.storyPoints}
                                                assigneeName={card.assigneeName}
                                                labels={card.labels}
                                                taskCount={card.taskCount}
                                                completedTaskCount={card.completedTaskCount}
                                                projectName={card.projectName}
                                            />
                                        </div>
                                    </DraggableCard>
                                ))}
                                {colCards.length === 0 && (
                                    <p className="py-4 text-center text-xs text-muted-foreground">No stories</p>
                                )}
                            </BoardColumn>
                        );
                    })}
                </div>

                <DragOverlay>
                    {activeItem && (
                        <StoryCardContent
                            storyKey={(activeItem as KanbanCard).storyKey}
                            title={(activeItem as KanbanCard).title}
                            priority={(activeItem as KanbanCard).priority}
                            storyPoints={(activeItem as KanbanCard).storyPoints}
                            assigneeName={(activeItem as KanbanCard).assigneeName}
                            labels={(activeItem as KanbanCard).labels}
                            isDragging
                        />
                    )}
                </DragOverlay>
            </DndContext>
        </div>
    );
}
