import { useDraggable } from '@dnd-kit/core';
import { Badge } from '@/components/common/Badge';
import { GripVertical } from 'lucide-react';
import type { ReactNode } from 'react';

interface DraggableCardProps {
    id: string;
    children: ReactNode;
}

export function DraggableCard({ id, children }: DraggableCardProps) {
    const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({ id });

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
            {children}
        </div>
    );
}

interface StoryCardContentProps {
    storyKey: string;
    title: string;
    priority: string;
    storyPoints?: number | null;
    assigneeName?: string | null;
    labels?: { labelId: string; name: string; color: string }[];
    taskCount?: number;
    completedTaskCount?: number;
    projectName?: string;
    isDragging?: boolean;
}

export function StoryCardContent({
    storyKey,
    title,
    priority,
    storyPoints,
    assigneeName,
    labels,
    taskCount,
    completedTaskCount,
    projectName,
    isDragging,
}: StoryCardContentProps) {
    return (
        <div className={`rounded-md border border-border bg-card p-2.5 ${isDragging ? 'shadow-lg ring-2 ring-primary' : 'hover:bg-accent/50'} cursor-grab`}>
            <div className="flex items-start gap-1.5">
                <GripVertical size={12} className="mt-0.5 shrink-0 text-muted-foreground" />
                <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-1.5">
                        <span className="text-[10px] font-medium text-muted-foreground">{storyKey}</span>
                        {projectName && <span className="text-[10px] text-muted-foreground">· {projectName}</span>}
                    </div>
                    <p className="text-sm text-foreground line-clamp-2">{title}</p>
                    <div className="mt-1.5 flex flex-wrap items-center gap-1">
                        <Badge variant="priority" value={priority} />
                        {storyPoints != null && (
                            <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">
                                {storyPoints} pts
                            </span>
                        )}
                        {labels && labels.length > 0 && labels.map((l) => (
                            <span
                                key={l.labelId}
                                className="h-2 w-2 rounded-full"
                                style={{ backgroundColor: l.color }}
                                title={l.name}
                            />
                        ))}
                    </div>
                    <div className="mt-1 flex items-center justify-between">
                        {taskCount != null && (
                            <span className="text-[10px] text-muted-foreground">
                                {completedTaskCount}/{taskCount} tasks
                            </span>
                        )}
                        {assigneeName && (
                            <span className="flex h-5 w-5 items-center justify-center rounded-full bg-primary text-[8px] font-medium text-primary-foreground" title={assigneeName}>
                                {assigneeName.split(' ').map((n) => n[0]).join('')}
                            </span>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}

interface TaskCardContentProps {
    taskTitle: string;
    storyKey?: string;
    taskType?: string;
    assigneeName?: string | null;
    departmentName?: string | null;
    priority: string;
    projectName?: string;
    isDragging?: boolean;
}

export function TaskCardContent({
    taskTitle,
    storyKey,
    taskType,
    assigneeName,
    departmentName,
    priority,
    projectName,
    isDragging,
}: TaskCardContentProps) {
    return (
        <div className={`rounded-md border border-border bg-card p-2.5 ${isDragging ? 'shadow-lg ring-2 ring-primary' : 'hover:bg-accent/50'} cursor-grab`}>
            <div className="flex items-start gap-1.5">
                <GripVertical size={12} className="mt-0.5 shrink-0 text-muted-foreground" />
                <div className="min-w-0 flex-1">
                    {storyKey && <span className="text-[10px] font-medium text-muted-foreground">{storyKey}</span>}
                    <p className="text-sm text-foreground line-clamp-2">{taskTitle}</p>
                    <div className="mt-1.5 flex flex-wrap items-center gap-1">
                        <Badge variant="priority" value={priority} />
                        {taskType && (
                            <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">{taskType}</span>
                        )}
                        {departmentName && (
                            <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">{departmentName}</span>
                        )}
                    </div>
                    <div className="mt-1 flex items-center justify-between">
                        {projectName && <span className="text-[10px] text-muted-foreground">{projectName}</span>}
                        {assigneeName && (
                            <span className="flex h-5 w-5 items-center justify-center rounded-full bg-primary text-[8px] font-medium text-primary-foreground" title={assigneeName}>
                                {assigneeName.split(' ').map((n) => n[0]).join('')}
                            </span>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
