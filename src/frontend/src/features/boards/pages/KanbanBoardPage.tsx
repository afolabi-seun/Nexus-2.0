import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { DndContext, DragOverlay } from '@dnd-kit/core';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { StoryStatus } from '@/types/enums';
import type { KanbanBoard, KanbanCard, BoardFilters as BoardFiltersType } from '@/types/work';
import { BoardColumn } from '../components/BoardColumn.js';
import { DraggableCard, StoryCardContent } from '../components/DraggableCard.js';
import { BoardFilters } from '../components/BoardFilters.js';
import { SaveFilterDialog } from '@/features/filters/components/SaveFilterDialog';
import { SavedFilterDropdown } from '@/features/filters/components/SavedFilterDropdown';
import { useBoardDragDrop } from '../hooks/useBoardDragDrop';

const KANBAN_COLUMNS = [
    StoryStatus.Backlog,
    StoryStatus.Ready,
    StoryStatus.InProgress,
    StoryStatus.InReview,
    StoryStatus.QA,
    StoryStatus.Done,
    StoryStatus.Closed,
];

export function KanbanBoardPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const [board, setBoard] = useState<KanbanBoard | null>(null);
    const [loading, setLoading] = useState(true);
    const [filters, setFilters] = useState<BoardFiltersType>({});

    const fetchBoard = useCallback(async () => {
        setLoading(true);
        try {
            const data = await workApi.getKanbanBoard(filters);
            setBoard(data);
        } catch {
            addToast('error', 'Failed to load kanban board');
        } finally {
            setLoading(false);
        }
    }, [filters, addToast]);

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
                <h1 className="text-2xl font-semibold text-foreground">Kanban Board</h1>
                <div className="flex items-center gap-2">
                    <SavedFilterDropdown onApply={(f) => setFilters(f as BoardFiltersType)} />
                    <SaveFilterDialog filters={filters} />
                    <BoardFilters filters={filters} onChange={setFilters} />
                </div>
            </div>

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
