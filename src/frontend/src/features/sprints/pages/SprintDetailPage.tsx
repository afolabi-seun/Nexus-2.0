import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { useAuth } from '@/hooks/useAuth';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { SprintStatus } from '@/types/enums';
import type { SprintDetail, SprintMetrics } from '@/types/work';
import type { StoryListItem } from '@/types/work';
import { SprintPlanningView } from '../components/SprintPlanningView.js';
import { SprintMetricsPanel } from '../components/SprintMetricsPanel.js';
import { BurndownChart } from '../components/BurndownChart.js';
import { ArrowLeft, Play, CheckCircle2, XCircle, Pencil } from 'lucide-react';

function SprintEditModal({
    open,
    onClose,
    sprint,
    onUpdated,
}: {
    open: boolean;
    onClose: () => void;
    sprint: SprintDetail;
    onUpdated: () => void;
}) {
    const { addToast } = useToast();
    const [name, setName] = useState(sprint.name);
    const [goal, setGoal] = useState(sprint.goal ?? '');
    const [startDate, setStartDate] = useState(sprint.startDate.slice(0, 10));
    const [endDate, setEndDate] = useState(sprint.endDate.slice(0, 10));
    const [saving, setSaving] = useState(false);
    const [errors, setErrors] = useState<Record<string, string>>({});

    useEffect(() => {
        if (open) {
            setName(sprint.name);
            setGoal(sprint.goal ?? '');
            setStartDate(sprint.startDate.slice(0, 10));
            setEndDate(sprint.endDate.slice(0, 10));
            setErrors({});
        }
    }, [open, sprint]);

    const handleSubmit = async () => {
        const newErrors: Record<string, string> = {};
        if (!name.trim()) newErrors.name = 'Name is required';
        if (startDate && endDate && startDate >= endDate) newErrors.endDate = 'End date must be after start date';
        if (Object.keys(newErrors).length > 0) {
            setErrors(newErrors);
            return;
        }

        setSaving(true);
        try {
            await workApi.updateSprint(sprint.sprintId, {
                name: name.trim(),
                goal: goal.trim() || undefined,
                startDate,
                endDate,
            });
            addToast('success', 'Sprint updated');
            onUpdated();
            onClose();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', 'Failed to update sprint');
            }
        } finally {
            setSaving(false);
        }
    };

    return (
        <Modal open={open} onClose={onClose} title="Edit Sprint">
            <div className="space-y-4">
                <div>
                    <label className="block text-sm font-medium text-foreground mb-1">Name</label>
                    <input
                        type="text"
                        value={name}
                        onChange={(e) => { setName(e.target.value); setErrors((prev) => ({ ...prev, name: '' })); }}
                        className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground"
                    />
                    {errors.name && <p className="mt-1 text-xs text-red-500">{errors.name}</p>}
                </div>
                <div>
                    <label className="block text-sm font-medium text-foreground mb-1">Goal</label>
                    <textarea
                        value={goal}
                        onChange={(e) => setGoal(e.target.value)}
                        rows={3}
                        className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground"
                    />
                </div>
                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm font-medium text-foreground mb-1">Start Date</label>
                        <input
                            type="date"
                            value={startDate}
                            onChange={(e) => { setStartDate(e.target.value); setErrors((prev) => ({ ...prev, endDate: '' })); }}
                            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-foreground mb-1">End Date</label>
                        <input
                            type="date"
                            value={endDate}
                            onChange={(e) => { setEndDate(e.target.value); setErrors((prev) => ({ ...prev, endDate: '' })); }}
                            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground"
                        />
                        {errors.endDate && <p className="mt-1 text-xs text-red-500">{errors.endDate}</p>}
                    </div>
                </div>
                <div className="flex justify-end gap-2 pt-2">
                    <button
                        onClick={onClose}
                        className="rounded-md border border-input px-3 py-2 text-sm font-medium text-foreground hover:bg-accent"
                    >
                        Cancel
                    </button>
                    <button
                        onClick={handleSubmit}
                        disabled={saving}
                        className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                    >
                        {saving ? 'Saving...' : 'Save Changes'}
                    </button>
                </div>
            </div>
        </Modal>
    );
}

export function SprintDetailPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { addToast } = useToast();
    const { user } = useAuth();

    const [sprint, setSprint] = useState<SprintDetail | null>(null);
    const [metrics, setMetrics] = useState<SprintMetrics | null>(null);
    const [loading, setLoading] = useState(true);
    const [actionLoading, setActionLoading] = useState(false);
    const [editOpen, setEditOpen] = useState(false);

    const canEdit = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead';

    const fetchSprint = useCallback(async () => {
        if (!id) return;
        setLoading(true);
        try {
            const [sprintData, metricsData] = await Promise.all([
                workApi.getSprint(id),
                workApi.getSprintMetrics(id).catch(() => null),
            ]);
            setSprint(sprintData);
            setMetrics(metricsData);
        } catch {
            addToast('error', 'Failed to load sprint');
        } finally {
            setLoading(false);
        }
    }, [id, addToast]);

    useEffect(() => { fetchSprint(); }, [fetchSprint]);

    // Auto-refresh metrics every 5 minutes when sprint is Active
    useEffect(() => {
        if (!id || sprint?.status !== SprintStatus.Active) return;
        const interval = setInterval(() => {
            workApi.getSprintMetrics(id).then(setMetrics).catch(() => { });
        }, 5 * 60 * 1000);
        return () => clearInterval(interval);
    }, [id, sprint?.status]);

    const handleLifecycleAction = async (action: 'start' | 'complete' | 'cancel') => {
        if (!id) return;
        setActionLoading(true);
        try {
            if (action === 'start') await workApi.startSprint(id);
            else if (action === 'complete') await workApi.completeSprint(id);
            else await workApi.cancelSprint(id);
            addToast('success', `Sprint ${action}ed`);
            fetchSprint();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', `Failed to ${action} sprint`);
            }
        } finally {
            setActionLoading(false);
        }
    };

    if (loading) return <SkeletonLoader variant="form" />;
    if (!sprint) return <div className="py-12 text-center text-muted-foreground">Sprint not found</div>;

    const storyColumns: Column<StoryListItem>[] = [
        { key: 'storyKey', header: 'Key' },
        { key: 'title', header: 'Title', render: (row) => <span className="truncate max-w-xs block">{row.title}</span> },
        { key: 'status', header: 'Status', render: (row) => <Badge variant="status" value={row.status} /> },
        { key: 'priority', header: 'Priority', render: (row) => <Badge variant="priority" value={row.priority} /> },
        { key: 'storyPoints', header: 'Points', render: (row) => String(row.storyPoints ?? '—') },
        { key: 'assigneeName', header: 'Assignee', render: (row) => row.assigneeName ?? 'Unassigned' },
    ];

    const completionPct = sprint.totalStoryPoints > 0
        ? Math.round((sprint.completedStoryPoints / sprint.totalStoryPoints) * 100)
        : 0;

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-start gap-3">
                <button
                    onClick={() => navigate('/sprints')}
                    className="mt-1 rounded-md p-1.5 text-muted-foreground hover:bg-accent"
                    aria-label="Back to sprints"
                >
                    <ArrowLeft size={18} />
                </button>
                <div className="flex-1 min-w-0">
                    <h1 className="text-2xl font-semibold text-foreground">{sprint.name}</h1>
                    <div className="mt-1 flex flex-wrap items-center gap-2">
                        <Badge variant="status" value={sprint.status} />
                        <span className="text-sm text-muted-foreground">{sprint.projectName}</span>
                        <span className="text-sm text-muted-foreground">
                            {new Date(sprint.startDate).toLocaleDateString()} – {new Date(sprint.endDate).toLocaleDateString()}
                        </span>
                    </div>
                    {sprint.goal && (
                        <p className="mt-2 text-sm text-muted-foreground">{sprint.goal}</p>
                    )}
                </div>
                <div className="flex gap-2 shrink-0">
                    {canEdit && (
                        <button
                            onClick={() => setEditOpen(true)}
                            className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-2 text-sm font-medium text-foreground hover:bg-accent"
                        >
                            <Pencil size={14} /> Edit
                        </button>
                    )}
                    {sprint.status === SprintStatus.Planning && (
                        <button
                            onClick={() => handleLifecycleAction('start')}
                            disabled={actionLoading}
                            className="inline-flex items-center gap-1.5 rounded-md bg-green-600 px-3 py-2 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
                        >
                            <Play size={14} /> Start Sprint
                        </button>
                    )}
                    {sprint.status === SprintStatus.Active && (
                        <>
                            <button
                                onClick={() => handleLifecycleAction('complete')}
                                disabled={actionLoading}
                                className="inline-flex items-center gap-1.5 rounded-md bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
                            >
                                <CheckCircle2 size={14} /> Complete
                            </button>
                            <button
                                onClick={() => handleLifecycleAction('cancel')}
                                disabled={actionLoading}
                                className="inline-flex items-center gap-1.5 rounded-md border border-red-300 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-50 dark:border-red-700 dark:text-red-400 dark:hover:bg-red-900/30 disabled:opacity-50"
                            >
                                <XCircle size={14} /> Cancel
                            </button>
                        </>
                    )}
                </div>
            </div>

            {/* Progress bar */}
            <div className="space-y-1">
                <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">
                        {sprint.completedStoryPoints}/{sprint.totalStoryPoints} story points · {sprint.stories.length} stories
                    </span>
                    <span className="font-medium text-foreground">{completionPct}%</span>
                </div>
                <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                    <div className="h-full rounded-full bg-primary transition-all" style={{ width: `${completionPct}%` }} />
                </div>
            </div>

            {/* Planning view for Planning status */}
            {sprint.status === SprintStatus.Planning && (
                <SprintPlanningView
                    sprint={sprint}
                    onUpdated={fetchSprint}
                />
            )}

            {/* Metrics & Burndown for Active/Completed */}
            {metrics && (sprint.status === SprintStatus.Active || sprint.status === SprintStatus.Completed) && (
                <>
                    <SprintMetricsPanel metrics={metrics} />
                    {metrics.burndownData.length > 0 && (
                        <BurndownChart data={metrics.burndownData} />
                    )}
                </>
            )}

            {/* Sprint stories table */}
            {sprint.stories.length > 0 && (
                <section className="space-y-2">
                    <h2 className="text-lg font-medium text-foreground">Sprint Stories</h2>
                    <DataTable
                        columns={storyColumns}
                        data={sprint.stories}
                        onRowClick={(row) => navigate(`/stories/${row.storyId}`)}
                        keyExtractor={(row) => row.storyId}
                    />
                </section>
            )}

            {/* Sprint Edit Modal */}
            {canEdit && sprint && (
                <SprintEditModal
                    open={editOpen}
                    onClose={() => setEditOpen(false)}
                    sprint={sprint}
                    onUpdated={fetchSprint}
                />
            )}
        </div>
    );
}
