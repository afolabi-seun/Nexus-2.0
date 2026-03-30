import { useState, useEffect } from 'react';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import { getValidTransitions } from '@/utils/workflowStateMachine';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { useAuth } from '@/hooks/useAuth';
import { MemberSelector } from '@/components/forms/MemberSelector';
import type { TaskDetail, ActivityLogEntry } from '@/types/work';
import type { TaskStatus } from '@/types/enums';
import { CommentSection } from '@/features/comments/components/CommentSection';
import { ActivityLog } from '@/features/activity/components/ActivityLog';
import { LogHoursDialog } from './LogHoursDialog.js';
import {
    X,
    ArrowRight,
    UserPlus,
    User,
    Clock,
    Calendar,
    Sparkles,
    Timer,
} from 'lucide-react';

interface TaskDetailPanelProps {
    task: TaskDetail;
    onClose: () => void;
    onTaskUpdated?: () => void;
}

export function TaskDetailPanel({ task, onClose, onTaskUpdated }: TaskDetailPanelProps) {
    const { addToast } = useToast();
    const { user } = useAuth();

    const [activity, setActivity] = useState<ActivityLogEntry[]>([]);
    const [transitioning, setTransitioning] = useState<string | null>(null);
    const [assignOpen, setAssignOpen] = useState(false);
    const [assigning, setAssigning] = useState(false);
    const [logHoursOpen, setLogHoursOpen] = useState(false);
    const [suggestedMemberId, setSuggestedMemberId] = useState<string | undefined>();
    const [suggesting, setSuggesting] = useState(false);

    const canAssign = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead';
    const canSelfAssign = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead' || user?.roleName === 'Member';
    const validTransitions = getValidTransitions('Task', task.status) as TaskStatus[];

    useEffect(() => {
        workApi.getTaskActivity(task.taskId).then(setActivity).catch(() => setActivity([]));
    }, [task.taskId]);

    const handleTransition = async (newStatus: TaskStatus) => {
        if (newStatus === 'InProgress' && !task.assigneeId) {
            addToast('error', 'Task must have an assignee before moving to In Progress');
            return;
        }
        setTransitioning(newStatus);
        try {
            await workApi.updateTaskStatus(task.taskId, { status: newStatus });
            addToast('success', `Task moved to ${newStatus}`);
            onTaskUpdated?.();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', 'Failed to update task status');
            }
        } finally {
            setTransitioning(null);
        }
    };

    const handleAssign = async (memberId: string) => {
        setAssigning(true);
        try {
            await workApi.assignTask(task.taskId, { assigneeId: memberId });
            addToast('success', 'Task assigned');
            setAssignOpen(false);
            onTaskUpdated?.();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', 'Failed to assign task');
            }
        } finally {
            setAssigning(false);
        }
    };

    const handleSelfAssign = async () => {
        try {
            await workApi.selfAssignTask(task.taskId);
            addToast('success', 'Task self-assigned');
            onTaskUpdated?.();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', 'Failed to self-assign task');
            }
        }
    };

    const handleSuggestAssignee = async () => {
        setSuggesting(true);
        try {
            const suggestion = await workApi.suggestAssignee({
                storyId: task.storyId,
                taskType: task.taskType,
            });
            setSuggestedMemberId(suggestion.memberId);
            addToast('info', `Suggested: ${suggestion.memberName} — ${suggestion.reason}`);
            setAssignOpen(true);
        } catch {
            addToast('error', 'Failed to get assignee suggestion');
        } finally {
            setSuggesting(false);
        }
    };

    return (
        <div className="fixed inset-y-0 right-0 z-50 flex w-full max-w-lg flex-col border-l border-border bg-card shadow-xl">
            {/* Header */}
            <div className="flex items-center justify-between border-b border-border px-4 py-3">
                <h2 className="text-lg font-semibold text-card-foreground truncate">{task.title}</h2>
                <button onClick={onClose} className="rounded-md p-1 text-muted-foreground hover:bg-accent" aria-label="Close">
                    <X size={18} />
                </button>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-4 space-y-5">
                {/* Status & badges */}
                <div className="flex flex-wrap items-center gap-2">
                    <Badge variant="status" value={task.status} />
                    <Badge variant="priority" value={task.priority} />
                    <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">{task.taskType}</span>
                </div>

                {/* Transition buttons */}
                {validTransitions.length > 0 && (
                    <div className="flex flex-wrap gap-2">
                        {validTransitions.map((status) => (
                            <button
                                key={status}
                                onClick={() => handleTransition(status)}
                                disabled={transitioning !== null}
                                className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                            >
                                <ArrowRight size={14} />
                                {transitioning === status ? 'Updating...' : `Move to ${status}`}
                            </button>
                        ))}
                    </div>
                )}

                {/* Assignment actions */}
                <div className="flex flex-wrap gap-2">
                    {canAssign && (
                        <button
                            onClick={() => setAssignOpen(true)}
                            className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-1.5 text-sm font-medium text-foreground hover:bg-accent"
                        >
                            <UserPlus size={14} />
                            {task.assigneeId ? 'Reassign' : 'Assign'}
                        </button>
                    )}
                    {canSelfAssign && !task.assigneeId && (
                        <button
                            onClick={handleSelfAssign}
                            className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-1.5 text-sm font-medium text-foreground hover:bg-accent"
                        >
                            <User size={14} /> Self-Assign
                        </button>
                    )}
                    <button
                        onClick={handleSuggestAssignee}
                        disabled={suggesting}
                        className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-1.5 text-sm font-medium text-foreground hover:bg-accent disabled:opacity-50"
                    >
                        <Sparkles size={14} /> {suggesting ? 'Suggesting...' : 'Suggest Assignee'}
                    </button>
                    <button
                        onClick={() => setLogHoursOpen(true)}
                        className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-1.5 text-sm font-medium text-foreground hover:bg-accent"
                    >
                        <Timer size={14} /> Log Hours
                    </button>
                </div>

                {/* Meta grid */}
                <div className="grid grid-cols-2 gap-3">
                    <MetaItem icon={<User size={14} />} label="Assignee" value={task.assigneeName ?? 'Unassigned'} />
                    <MetaItem label="Department" value={task.departmentName ?? 'Auto-mapped'} />
                    <MetaItem icon={<Clock size={14} />} label="Estimated Hours" value={task.estimatedHours != null ? `${task.estimatedHours}h` : '—'} />
                    <MetaItem icon={<Clock size={14} />} label="Actual Hours" value={task.actualHours != null ? `${task.actualHours}h` : '—'} />
                    <MetaItem icon={<Calendar size={14} />} label="Due Date" value={task.dueDate ? new Date(task.dueDate).toLocaleDateString() : 'None'} />
                    <MetaItem label="Story" value={task.storyKey} />
                    <MetaItem icon={<Calendar size={14} />} label="Created" value={new Date(task.dateCreated).toLocaleDateString()} />
                    {task.completedDate && (
                        <MetaItem label="Completed" value={new Date(task.completedDate).toLocaleDateString()} />
                    )}
                </div>

                {/* Description */}
                {task.description && (
                    <section className="space-y-1">
                        <h3 className="text-sm font-medium text-muted-foreground">Description</h3>
                        <div className="rounded-md border border-border bg-background p-3 text-sm text-foreground whitespace-pre-wrap">
                            {task.description}
                        </div>
                    </section>
                )}

                {/* Comments */}
                <CommentSection entityType="Task" entityId={task.taskId} />

                {/* Activity Log */}
                <section className="space-y-2">
                    <h3 className="text-sm font-medium text-muted-foreground">Activity Log</h3>
                    <ActivityLog entries={activity} />
                </section>
            </div>

            {/* Assign Modal */}
            <Modal open={assignOpen} onClose={() => { setAssignOpen(false); setSuggestedMemberId(undefined); }} title="Assign Task">
                <div className="space-y-4">
                    <MemberSelector
                        value={suggestedMemberId ?? task.assigneeId ?? undefined}
                        onSelect={(memberId) => handleAssign(memberId)}
                        departmentId={task.departmentId ?? undefined}
                        placeholder="Search for a member..."
                    />
                    {assigning && <p className="text-sm text-muted-foreground">Assigning...</p>}
                </div>
            </Modal>

            {/* Log Hours Dialog */}
            <LogHoursDialog
                open={logHoursOpen}
                onClose={() => setLogHoursOpen(false)}
                taskId={task.taskId}
                onSuccess={() => { setLogHoursOpen(false); onTaskUpdated?.(); }}
            />
        </div>
    );
}

function MetaItem({ icon, label, value }: { icon?: React.ReactNode; label: string; value: string }) {
    return (
        <div className="rounded-lg border border-border bg-background p-2.5">
            <div className="flex items-center gap-1 text-xs text-muted-foreground">
                {icon} {label}
            </div>
            <p className="mt-0.5 text-sm font-medium text-foreground truncate">{value}</p>
        </div>
    );
}
