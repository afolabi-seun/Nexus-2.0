import { useState, useEffect, useCallback, useMemo } from 'react';
import { DndContext, DragOverlay } from '@dnd-kit/core';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { ListFilter } from '@/components/common/ListFilter';
import { useListFilters } from '@/hooks/useListFilters';
import { TaskStatus, Priority } from '@/types/enums';
import type { FilterConfig } from '@/types/filters';
import type { SprintBoard, SprintBoardCard } from '@/types/work';
import { BoardColumn } from '../components/BoardColumn.js';
import { DraggableCard, TaskCardContent } from '../components/DraggableCard.js';
import { useBoardDragDrop } from '../hooks/useBoardDragDrop';
import { PageHeader } from '@/components/common/PageHeader';

const SPRINT_COLUMNS = [
    TaskStatus.ToDo,
    TaskStatus.InProgress,
    TaskStatus.InReview,
    TaskStatus.Done,
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

export function SprintBoardPage() {
    const { addToast } = useToast();
    const [board, setBoard] = useState<SprintBoard | null>(null);
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
            const data = await workApi.getSprintBoard(apiFilters);
            setBoard(data);
        } catch {
            addToast('error', 'Failed to load sprint board');
        } finally {
            setLoading(false);
        }
    }, [apiFilters, addToast]);

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
                <PageHeader title="Sprint Board" description="View and manage tasks in the active sprint. Drag tasks between status columns." dismissKey="sprint-board" />
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
                    <PageHeader title="Sprint Board" description="View and manage tasks in the active sprint. Drag tasks between status columns." dismissKey="sprint-board" />
                    {board?.sprintName && (
                        <p className="text-sm text-muted-foreground">
                            {board.sprintName}
                            {board.projectName && ` · ${board.projectName}`}
                        </p>
                    )}
                </div>
                <div className="flex items-center gap-2">
                    <ListFilter
                        configs={filterConfigs}
                        values={filterValues}
                        onUpdateFilter={updateFilter}
                        onClearFilters={clearFilters}
                        hasActiveFilters={hasActiveFilters}
                        activeFilterCount={activeFilterCount}
                        enableSavedFilters
                    />
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
