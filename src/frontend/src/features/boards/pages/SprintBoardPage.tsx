import { useState, useEffect, useCallback } from 'react';
import { DndContext, DragOverlay } from '@dnd-kit/core';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { TaskStatus } from '@/types/enums';
import type { SprintBoard, SprintBoardCard, BoardFilters as BoardFiltersType } from '@/types/work';
import { BoardColumn } from '../components/BoardColumn.js';
import { DraggableCard, TaskCardContent } from '../components/DraggableCard.js';
import { BoardFilters } from '../components/BoardFilters.js';
import { SaveFilterDialog } from '@/features/filters/components/SaveFilterDialog';
import { SavedFilterDropdown } from '@/features/filters/components/SavedFilterDropdown';
import { useBoardDragDrop } from '../hooks/useBoardDragDrop';

const SPRINT_COLUMNS = [
    TaskStatus.ToDo,
    TaskStatus.InProgress,
    TaskStatus.InReview,
    TaskStatus.Done,
];

export function SprintBoardPage() {
    const { addToast } = useToast();
    const [board, setBoard] = useState<SprintBoard | null>(null);
    const [loading, setLoading] = useState(true);
    const [filters, setFilters] = useState<BoardFiltersType>({});

    const fetchBoard = useCallback(async () => {
        setLoading(true);
        try {
            const data = await workApi.getSprintBoard(filters);
            setBoard(data);
        } catch {
            addToast('error', 'Failed to load sprint board');
        } finally {
            setLoading(false);
        }
    }, [filters, addToast]);

    useEffect(() => { fetchBoard(); }, [fetchBoard]);

    const allCards: (SprintBoardCard & { _status: TaskStatus })[] = board
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
        getItemId: (c) => c.taskId,
        getItemColumn: (c) => c._status,
        onMove: async (itemId, _from, toColumn) => {
            await workApi.updateTaskStatus(itemId, { status: toColumn as TaskStatus });
        },
        onRefresh: fetchBoard,
    });

    if (loading) return <SkeletonLoader variant="table" rows={5} columns={4} />;

    if (board && !board.hasActiveSprint) {
        return (
            <div className="space-y-4">
                <h1 className="text-2xl font-semibold text-foreground">Sprint Board</h1>
                <div className="py-12 text-center text-muted-foreground">
                    {board.message ?? 'No active sprint. Start a sprint to see the board.'}
                </div>
            </div>
        );
    }

    const cardsByColumn: Record<string, typeof allCards> = {};
    for (const col of SPRINT_COLUMNS) {
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
                <div>
                    <h1 className="text-2xl font-semibold text-foreground">Sprint Board</h1>
                    {board?.sprintName && (
                        <p className="text-sm text-muted-foreground">
                            {board.sprintName}
                            {board.projectName && ` · ${board.projectName}`}
                        </p>
                    )}
                </div>
                <div className="flex items-center gap-2">
                    <SavedFilterDropdown onApply={(f) => setFilters(f as BoardFiltersType)} />
                    <SaveFilterDialog filters={filters} />
                    <BoardFilters filters={filters} onChange={setFilters} />
                </div>
            </div>

            <DndContext sensors={sensors} onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
                <div className="flex gap-3 overflow-x-auto pb-4">
                    {SPRINT_COLUMNS.map((status) => {
                        const colCards = cardsByColumn[status] ?? [];
                        return (
                            <BoardColumn
                                key={status}
                                id={status}
                                name={status}
                                cardCount={colCards.length}
                            >
                                {colCards.map((card) => (
                                    <DraggableCard key={card.taskId} id={card.taskId}>
                                        <TaskCardContent
                                            taskTitle={card.taskTitle}
                                            storyKey={card.storyKey}
                                            taskType={card.taskType}
                                            assigneeName={card.assigneeName}
                                            departmentName={card.departmentName}
                                            priority={card.priority}
                                            projectName={card.projectName}
                                        />
                                    </DraggableCard>
                                ))}
                                {colCards.length === 0 && (
                                    <p className="py-4 text-center text-xs text-muted-foreground">No tasks</p>
                                )}
                            </BoardColumn>
                        );
                    })}
                </div>

                <DragOverlay>
                    {activeItem && (
                        <TaskCardContent
                            taskTitle={(activeItem as SprintBoardCard).taskTitle}
                            storyKey={(activeItem as SprintBoardCard).storyKey}
                            taskType={(activeItem as SprintBoardCard).taskType}
                            assigneeName={(activeItem as SprintBoardCard).assigneeName}
                            departmentName={(activeItem as SprintBoardCard).departmentName}
                            priority={(activeItem as SprintBoardCard).priority}
                            isDragging
                        />
                    )}
                </DragOverlay>
            </DndContext>
        </div>
    );
}
