import type { ActivityLogEntry } from '@/types/work';
import {
    ArrowRight,
    User,
    MessageSquare,
    Tag,
    Pencil,
} from 'lucide-react';

interface ActivityLogProps {
    entries: ActivityLogEntry[];
}

const actionIcons: Record<string, React.ReactNode> = {
    StatusChanged: <ArrowRight size={14} className="text-blue-500" />,
    Assigned: <User size={14} className="text-green-500" />,
    Unassigned: <User size={14} className="text-orange-500" />,
    Commented: <MessageSquare size={14} className="text-purple-500" />,
    LabelAdded: <Tag size={14} className="text-teal-500" />,
    LabelRemoved: <Tag size={14} className="text-red-500" />,
    Created: <Pencil size={14} className="text-gray-500" />,
    Updated: <Pencil size={14} className="text-yellow-500" />,
};

function getIcon(action: string): React.ReactNode {
    // Try exact match first, then partial match
    if (actionIcons[action]) return actionIcons[action];
    const lower = action.toLowerCase();
    if (lower.includes('status')) return actionIcons.StatusChanged;
    if (lower.includes('assign')) return actionIcons.Assigned;
    if (lower.includes('comment')) return actionIcons.Commented;
    if (lower.includes('label')) return actionIcons.LabelAdded;
    return actionIcons.Updated;
}

export function ActivityLog({ entries }: ActivityLogProps) {
    if (entries.length === 0) {
        return <p className="text-sm text-muted-foreground">No activity yet</p>;
    }

    return (
        <div className="relative space-y-0">
            {/* Timeline line */}
            <div className="absolute left-[15px] top-2 bottom-2 w-px bg-border" />

            {entries.map((entry) => (
                <div key={entry.activityLogId} className="relative flex gap-3 py-2">
                    {/* Icon circle */}
                    <div className="relative z-10 flex h-8 w-8 shrink-0 items-center justify-center rounded-full border border-border bg-card">
                        {getIcon(entry.action)}
                    </div>

                    {/* Content */}
                    <div className="min-w-0 flex-1 pt-0.5">
                        <p className="text-sm text-foreground">
                            <span className="font-medium">{entry.actorName}</span>{' '}
                            {entry.description}
                        </p>
                        {(entry.oldValue || entry.newValue) && (
                            <p className="mt-0.5 text-xs text-muted-foreground">
                                {entry.oldValue && (
                                    <span className="line-through">{entry.oldValue}</span>
                                )}
                                {entry.oldValue && entry.newValue && ' → '}
                                {entry.newValue && (
                                    <span className="font-medium text-foreground">{entry.newValue}</span>
                                )}
                            </p>
                        )}
                        <p className="mt-0.5 text-xs text-muted-foreground">
                            {new Date(entry.dateCreated).toLocaleString()}
                        </p>
                    </div>
                </div>
            ))}
        </div>
    );
}
