import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { useAuth } from '@/hooks/useAuth';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { StoryDetail, TaskDetail, ActivityLogEntry } from '@/types/work';
import { StatusTransitionButtons } from '../components/StatusTransitionButtons.js';
import { LabelManager } from '../components/LabelManager.js';
import { StoryLinkDialog } from '../components/StoryLinkDialog.js';
import { StoryForm } from '../components/StoryForm.js';
import { StoryTimeEntries } from '../components/StoryTimeEntries';
import { CommentSection } from '@/features/comments/components/CommentSection';
import { ActivityLog } from '@/features/activity/components/ActivityLog';
import {
    ArrowLeft,
    Pencil,
    Calendar,
    User,
    Clock,
    CheckCircle2,
    Plus,
    ListTodo,
    Trash2,
    UserX,
} from 'lucide-react';

export function StoryDetailPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { addToast } = useToast();

    const [story, setStory] = useState<StoryDetail | null>(null);
    const [activity, setActivity] = useState<ActivityLogEntry[]>([]);
    const [loading, setLoading] = useState(true);
    const [editOpen, setEditOpen] = useState(false);
    const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
    const [taskDeleteConfirmOpen, setTaskDeleteConfirmOpen] = useState(false);
    const [pendingTaskDeleteId, setPendingTaskDeleteId] = useState<string | null>(null);

    const { user } = useAuth();
    const canDelete = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead';

    const fetchStory = useCallback(async () => {
        if (!id) return;
        setLoading(true);
        try {
            const [storyData, activityData] = await Promise.all([
                workApi.getStory(id),
                workApi.getStoryActivity(id).catch(() => [] as ActivityLogEntry[]),
            ]);
            setStory(storyData);
            setActivity(activityData);
        } catch {
            addToast('error', 'Failed to load story');
        } finally {
            setLoading(false);
        }
    }, [id, addToast]);

    useEffect(() => { fetchStory(); }, [fetchStory]);

    if (loading) return <SkeletonLoader variant="form" />;
    if (!story) return <div className="py-12 text-center text-muted-foreground">Story not found</div>;

    const completionPct = story.totalTaskCount > 0 ? Math.round(story.completionPercentage) : 0;

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-start gap-3">
                <button
                    onClick={() => navigate('/stories')}
                    className="mt-1 rounded-md p-1.5 text-muted-foreground hover:bg-accent"
                    aria-label="Back to stories"
                >
                    <ArrowLeft size={18} />
                </button>
                <div className="flex-1 min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                        <span className="text-sm font-medium text-muted-foreground">{story.storyKey}</span>
                        <h1 className="text-2xl font-semibold text-foreground">{story.title}</h1>
                    </div>
                    <div className="mt-1 flex flex-wrap items-center gap-2">
                        <Badge variant="status" value={story.status} />
                        <Badge variant="priority" value={story.priority} />
                        {story.storyPoints != null && (
                            <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
                                {story.storyPoints} pts
                            </span>
                        )}
                    </div>
                </div>
                <button
                    onClick={() => setEditOpen(true)}
                    className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-2 text-sm font-medium text-foreground hover:bg-accent"
                >
                    <Pencil size={14} /> Edit
                </button>
                {canDelete && (
                    <button
                        onClick={() => setDeleteConfirmOpen(true)}
                        className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-2 text-sm font-medium text-destructive hover:bg-accent"
                        aria-label="Delete story"
                    >
                        <Trash2 size={14} />
                    </button>
                )}
            </div>

            {/* Status Transitions */}
            <StatusTransitionButtons
                storyId={story.storyId}
                currentStatus={story.status}
                assigneeId={story.assigneeId}
                onStatusChanged={fetchStory}
            />

            {/* Meta grid */}
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
                <MetaItem icon={<User size={14} />} label="Assignee" value={story.assigneeName ?? 'Unassigned'}>
                    {story.assigneeId && (
                        <button
                            onClick={async () => {
                                try {
                                    await workApi.unassignStory(story.storyId);
                                    addToast('success', 'Story unassigned');
                                    fetchStory();
                                } catch (err) {
                                    if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
                                    else addToast('error', 'Failed to unassign story');
                                }
                            }}
                            className="ml-1 text-muted-foreground hover:text-foreground"
                            aria-label="Unassign story"
                        >
                            <UserX size={14} />
                        </button>
                    )}
                </MetaItem>
                <MetaItem icon={<User size={14} />} label="Reporter" value={story.reporterName ?? '—'} />
                <MetaItem label="Project" value={`${story.projectName} (${story.projectKey})`} />
                <MetaItem label="Sprint" value={story.sprintName ?? 'None'} />
                <MetaItem label="Department" value={story.departmentName ?? 'None'} />
                <MetaItem icon={<Calendar size={14} />} label="Due Date" value={story.dueDate ? new Date(story.dueDate).toLocaleDateString() : 'None'} />
                <MetaItem icon={<Clock size={14} />} label="Created" value={new Date(story.dateCreated).toLocaleDateString()} />
                <MetaItem icon={<Clock size={14} />} label="Updated" value={new Date(story.dateUpdated).toLocaleDateString()} />
                {story.completedDate && (
                    <MetaItem icon={<CheckCircle2 size={14} />} label="Completed" value={new Date(story.completedDate).toLocaleDateString()} />
                )}
            </div>

            {/* Description */}
            {story.description && (
                <section className="space-y-1">
                    <h2 className="text-sm font-medium text-muted-foreground">Description</h2>
                    <div className="rounded-md border border-border bg-card p-4 text-sm text-card-foreground whitespace-pre-wrap">
                        {story.description}
                    </div>
                </section>
            )}

            {/* Acceptance Criteria */}
            {story.acceptanceCriteria && (
                <section className="space-y-1">
                    <h2 className="text-sm font-medium text-muted-foreground">Acceptance Criteria</h2>
                    <div className="rounded-md border border-border bg-card p-4 text-sm text-card-foreground whitespace-pre-wrap">
                        {story.acceptanceCriteria}
                    </div>
                </section>
            )}

            {/* Completion Progress */}
            <section className="space-y-2">
                <div className="flex items-center justify-between">
                    <h2 className="text-sm font-medium text-muted-foreground">
                        Completion ({story.completedTaskCount}/{story.totalTaskCount} tasks)
                    </h2>
                    <span className="text-sm font-medium text-foreground">{completionPct}%</span>
                </div>
                <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                    <div
                        className="h-full rounded-full bg-primary transition-all"
                        style={{ width: `${completionPct}%` }}
                    />
                </div>

                {/* Department contributions */}
                {story.departmentContributions.length > 0 && (
                    <div className="mt-2 space-y-1">
                        <p className="text-xs text-muted-foreground">Department Contributions</p>
                        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                            {story.departmentContributions.map((dc) => (
                                <div key={dc.departmentName} className="rounded border border-border px-3 py-2">
                                    <p className="text-xs font-medium text-foreground">{dc.departmentName}</p>
                                    <p className="text-xs text-muted-foreground">
                                        {dc.completedTaskCount}/{dc.taskCount} tasks
                                    </p>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </section>

            {/* Tasks */}
            <section className="space-y-2">
                <div className="flex items-center justify-between">
                    <h2 className="text-lg font-medium text-foreground flex items-center gap-2">
                        <ListTodo size={18} /> Tasks
                    </h2>
                    <button
                        onClick={() => navigate(`/stories/${story.storyId}`, { state: { createTask: true } })}
                        className="inline-flex items-center gap-1 rounded-md border border-input px-2 py-1 text-xs font-medium text-foreground hover:bg-accent"
                    >
                        <Plus size={12} /> Create Task
                    </button>
                </div>
                {story.tasks.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No tasks yet</p>
                ) : (
                    <div className="space-y-1.5">
                        {story.tasks.map((task) => (
                            <TaskRow
                                key={task.taskId}
                                task={task}
                                canDelete={canDelete}
                                onDelete={(taskId) => {
                                    setPendingTaskDeleteId(taskId);
                                    setTaskDeleteConfirmOpen(true);
                                }}
                                onUnassign={async (taskId) => {
                                    try {
                                        await workApi.unassignTask(taskId);
                                        addToast('success', 'Task unassigned');
                                        fetchStory();
                                    } catch (err) {
                                        if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
                                        else addToast('error', 'Failed to unassign task');
                                    }
                                }}
                            />
                        ))}
                    </div>
                )}
            </section>

            {/* Time Logged */}
            <section className="space-y-2">
                <h2 className="text-lg font-medium text-foreground flex items-center gap-2">
                    <Clock size={18} /> Time Logged
                </h2>
                <StoryTimeEntries storyId={story.storyId} />
            </section>

            {/* Labels */}
            <section className="space-y-2">
                <h2 className="text-lg font-medium text-foreground">Labels</h2>
                <LabelManager
                    storyId={story.storyId}
                    appliedLabels={story.labels}
                    onLabelsChanged={fetchStory}
                />
            </section>

            {/* Links */}
            <section className="space-y-2">
                <h2 className="text-lg font-medium text-foreground">Linked Stories</h2>
                <StoryLinkDialog
                    storyId={story.storyId}
                    links={story.links}
                    onLinksChanged={fetchStory}
                />
            </section>

            {/* Activity Log */}
            <section className="space-y-2">
                <h2 className="text-lg font-medium text-foreground">Activity Log</h2>
                <ActivityLog entries={activity} />
            </section>

            {/* Comments */}
            <CommentSection entityType="Story" entityId={story.storyId} />

            {/* Edit Modal */}
            <Modal open={editOpen} onClose={() => setEditOpen(false)} title="Edit Story">
                <StoryForm
                    mode="edit"
                    story={story}
                    onSuccess={(updated) => {
                        setStory(updated);
                        setEditOpen(false);
                    }}
                />
            </Modal>

            {/* Delete Story Confirm Dialog */}
            <ConfirmDialog
                open={deleteConfirmOpen}
                title="Delete Story"
                message="This story will be soft-deleted. Are you sure?"
                onConfirm={async () => {
                    try {
                        await workApi.deleteStory(story.storyId);
                        addToast('success', 'Story deleted');
                        navigate('/stories');
                    } catch (err) {
                        if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
                        else addToast('error', 'Failed to delete story');
                    } finally {
                        setDeleteConfirmOpen(false);
                    }
                }}
                onCancel={() => setDeleteConfirmOpen(false)}
            />

            {/* Delete Task Confirm Dialog */}
            <ConfirmDialog
                open={taskDeleteConfirmOpen}
                title="Delete Task"
                message="This task will be soft-deleted. Are you sure?"
                onConfirm={async () => {
                    try {
                        await workApi.deleteTask(pendingTaskDeleteId!);
                        addToast('success', 'Task deleted');
                        fetchStory();
                    } catch (err) {
                        if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
                        else addToast('error', 'Failed to delete task');
                    } finally {
                        setPendingTaskDeleteId(null);
                        setTaskDeleteConfirmOpen(false);
                    }
                }}
                onCancel={() => {
                    setPendingTaskDeleteId(null);
                    setTaskDeleteConfirmOpen(false);
                }}
            />
        </div>
    );
}

function MetaItem({ icon, label, value, children }: { icon?: React.ReactNode; label: string; value: string; children?: React.ReactNode }) {
    return (
        <div className="rounded-lg border border-border bg-card p-3">
            <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                {icon}
                {label}
            </div>
            <div className="mt-0.5 flex items-center gap-1">
                <p className="text-sm font-medium text-card-foreground truncate">{value}</p>
                {children}
            </div>
        </div>
    );
}

function TaskRow({ task, canDelete, onDelete, onUnassign }: { task: TaskDetail; canDelete: boolean; onDelete: (taskId: string) => void; onUnassign: (taskId: string) => void }) {
    return (
        <div className="flex items-center justify-between rounded-md border border-border px-3 py-2">
            <div className="flex items-center gap-2 min-w-0">
                <Badge variant="status" value={task.status} />
                <span className="text-sm text-foreground truncate">{task.title}</span>
            </div>
            <div className="flex items-center gap-2 shrink-0">
                <Badge variant="priority" value={task.priority} />
                <span className="text-xs text-muted-foreground">{task.assigneeName ?? 'Unassigned'}</span>
                {task.assigneeId && (
                    <button
                        onClick={() => onUnassign(task.taskId)}
                        className="text-muted-foreground hover:text-foreground"
                        aria-label="Unassign task"
                    >
                        <UserX size={14} />
                    </button>
                )}
                {canDelete && (
                    <button
                        onClick={() => onDelete(task.taskId)}
                        className="text-muted-foreground hover:text-destructive"
                        aria-label="Delete task"
                    >
                        <Trash2 size={14} />
                    </button>
                )}
            </div>
        </div>
    );
}
