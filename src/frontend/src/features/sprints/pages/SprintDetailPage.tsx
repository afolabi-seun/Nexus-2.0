import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import { DataTable, type Column } from '@/components/common/DataTable';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { SprintStatus } from '@/types/enums';
import type { SprintDetail, SprintMetrics } from '@/types/work';
import type { StoryListItem } from '@/types/work';
import { SprintPlanningView } from '../components/SprintPlanningView.js';
import { SprintMetricsPanel } from '../components/SprintMetricsPanel.js';
import { BurndownChart } from '../components/BurndownChart.js';
import { ArrowLeft, Play, CheckCircle2, XCircle } from 'lucide-react';

export function SprintDetailPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { addToast } = useToast();

    const [sprint, setSprint] = useState<SprintDetail | null>(null);
    const [metrics, setMetrics] = useState<SprintMetrics | null>(null);
    const [loading, setLoading] = useState(true);
    const [actionLoading, setActionLoading] = useState(false);

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
        </div>
    );
}
