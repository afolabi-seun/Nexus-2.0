import { useDroppable } from '@dnd-kit/core';
import type { ReactNode } from 'react';

interface BoardColumnProps {
    id: string;
    name: string;
    cardCount: number;
    totalPoints?: number;
    children: ReactNode;
}

export function BoardColumn({ id, name, cardCount, totalPoints, children }: BoardColumnProps) {
    const { setNodeRef, isOver } = useDroppable({ id });

    return (
        <div
            ref={setNodeRef}
            className={`flex min-w-[260px] flex-col rounded-lg border-2 ${isOver ? 'border-primary bg-primary/5' : 'border-border bg-muted/30'} transition-colors`}
        >
            <div className="flex items-center justify-between border-b border-border px-3 py-2">
                <h3 className="text-sm font-semibold text-foreground">{name}</h3>
                <div className="flex items-center gap-1.5">
                    {totalPoints != null && (
                        <span className="text-[10px] text-muted-foreground">{totalPoints} pts</span>
                    )}
                    <span className="rounded-full bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">
                        {cardCount}
                    </span>
                </div>
            </div>
            <div className="flex-1 space-y-1.5 overflow-y-auto p-2" style={{ maxHeight: 'calc(100vh - 240px)' }}>
                {children}
            </div>
        </div>
    );
}
