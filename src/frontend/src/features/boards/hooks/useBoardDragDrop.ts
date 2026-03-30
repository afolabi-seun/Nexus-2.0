import { useState, useCallback } from 'react';
import {
    PointerSensor,
    useSensor,
    useSensors,
    type DragStartEvent,
    type DragEndEvent,
} from '@dnd-kit/core';
import { useToast } from '@/components/common/Toast';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';

interface UseBoardDragDropOptions<T> {
    items: T[];
    getItemId: (item: T) => string;
    getItemColumn: (item: T) => string;
    onMove: (itemId: string, fromColumn: string, toColumn: string) => Promise<void>;
    onRefresh: () => void;
}

export function useBoardDragDrop<T>({
    items,
    getItemId,
    getItemColumn,
    onMove,
    onRefresh,
}: UseBoardDragDropOptions<T>) {
    const { addToast } = useToast();
    const [activeItem, setActiveItem] = useState<T | null>(null);
    const [optimisticOverrides, setOptimisticOverrides] = useState<Record<string, string>>({});

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 5 } })
    );

    const handleDragStart = useCallback(
        (event: DragStartEvent) => {
            const id = String(event.active.id);
            const item = items.find((i) => getItemId(i) === id);
            setActiveItem(item ?? null);
        },
        [items, getItemId]
    );

    const handleDragEnd = useCallback(
        async (event: DragEndEvent) => {
            setActiveItem(null);
            const { active, over } = event;
            if (!over) return;

            const itemId = String(active.id);
            const targetColumn = String(over.id);
            const item = items.find((i) => getItemId(i) === itemId);
            if (!item) return;

            const fromColumn = getItemColumn(item);
            if (fromColumn === targetColumn) return;

            // Optimistic update
            setOptimisticOverrides((prev) => ({ ...prev, [itemId]: targetColumn }));

            try {
                await onMove(itemId, fromColumn, targetColumn);
                onRefresh();
            } catch (err) {
                // Revert
                setOptimisticOverrides((prev) => {
                    const next = { ...prev };
                    delete next[itemId];
                    return next;
                });
                if (err instanceof ApiError) {
                    addToast('error', mapErrorCode(err.errorCode));
                } else {
                    addToast('error', 'Failed to move item');
                }
            }
        },
        [items, getItemId, getItemColumn, onMove, onRefresh, addToast]
    );

    const getEffectiveColumn = useCallback(
        (item: T): string => {
            const id = getItemId(item);
            return optimisticOverrides[id] ?? getItemColumn(item);
        },
        [getItemId, getItemColumn, optimisticOverrides]
    );

    return {
        sensors,
        activeItem,
        handleDragStart,
        handleDragEnd,
        getEffectiveColumn,
    };
}
