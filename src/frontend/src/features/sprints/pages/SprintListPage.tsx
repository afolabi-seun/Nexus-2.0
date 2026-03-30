import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { Pagination } from '@/components/common/Pagination';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { usePagination } from '@/hooks/usePagination';
import { useAuth } from '@/hooks/useAuth';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { SprintStatus } from '@/types/enums';
import type { SprintListItem, ProjectListItem } from '@/types/work';
import { SprintForm } from '../components/SprintForm.js';
import { Plus, Play, CheckCircle2, XCircle } from 'lucide-react';

export function SprintListPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const { user } = useAuth();
    const { page, pageSize, setPage, setPageSize } = usePagination();

    const [sprints, setSprints] = useState<SprintListItem[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [loading, setLoading] = useState(true);
    const [createOpen, setCreateOpen] = useState(false);
    const [projects, setProjects] = useState<ProjectListItem[]>([]);
    const [projectFilter, setProjectFilter] = useState('');
    const [actionLoading, setActionLoading] = useState<string | null>(null);

    const canCreate = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead';

    const fetchSprints = useCallback(async () => {
        setLoading(true);
        try {
            const res = await workApi.getSprints({
                page,
                pageSize,
                projectId: projectFilter || undefined,
            });
            setSprints(res.data);
            setTotalCount(res.totalCount);
        } catch {
            addToast('error', 'Failed to load sprints');
        } finally {
            setLoading(false);
        }
    }, [page, pageSize, projectFilter, addToast]);

    useEffect(() => {
        workApi.getProjects({ page: 1, pageSize: 100 }).then((r) => setProjects(r.data)).catch(() => { });
    }, []);

    useEffect(() => { fetchSprints(); }, [fetchSprints]);

    const handleLifecycleAction = async (sprintId: string, action: 'start' | 'complete' | 'cancel') => {
        setActionLoading(sprintId);
        try {
            if (action === 'start') await workApi.startSprint(sprintId);
            else if (action === 'complete') await workApi.completeSprint(sprintId);
            else await workApi.cancelSprint(sprintId);
            addToast('success', `Sprint ${action}ed`);
            fetchSprints();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', `Failed to ${action} sprint`);
            }
        } finally {
            setActionLoading(null);
        }
    };

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-semibold text-foreground">Sprints</h1>
                {canCreate && (
                    <button
                        onClick={() => setCreateOpen(true)}
                        className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        <Plus size={16} /> Create Sprint
                    </button>
                )}
            </div>

            {/* Filters */}
            <div className="flex items-center gap-3">
                <select
                    value={projectFilter}
                    onChange={(e) => { setProjectFilter(e.target.value); setPage(1); }}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                >
                    <option value="">All Projects</option>
                    {projects.map((p) => (
                        <option key={p.projectId} value={p.projectId}>{p.name}</option>
                    ))}
                </select>
            </div>

            {loading ? (
                <SkeletonLoader variant="table" rows={5} columns={7} />
            ) : sprints.length === 0 ? (
                <div className="py-12 text-center text-muted-foreground">No sprints found</div>
            ) : (
                <div className="overflow-x-auto rounded-md border border-border">
                    <table className="w-full text-sm">
                        <thead>
                            <tr className="border-b border-border bg-muted/50">
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Sprint Name</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Project</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Status</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Dates</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Stories</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Velocity</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {sprints.map((sprint) => (
                                <tr
                                    key={sprint.sprintId}
                                    className="border-b border-border last:border-0 cursor-pointer hover:bg-muted/50"
                                    onClick={() => navigate(`/sprints/${sprint.sprintId}`)}
                                >
                                    <td className="px-4 py-3 font-medium text-foreground">{sprint.name}</td>
                                    <td className="px-4 py-3 text-muted-foreground">{sprint.projectName}</td>
                                    <td className="px-4 py-3"><Badge variant="status" value={sprint.status} /></td>
                                    <td className="px-4 py-3 text-muted-foreground">
                                        {new Date(sprint.startDate).toLocaleDateString()} – {new Date(sprint.endDate).toLocaleDateString()}
                                    </td>
                                    <td className="px-4 py-3 text-foreground">{sprint.storyCount}</td>
                                    <td className="px-4 py-3 text-foreground">{sprint.velocity ?? '—'}</td>
                                    <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                                        <div className="flex gap-1">
                                            {sprint.status === SprintStatus.Planning && (
                                                <button
                                                    onClick={() => handleLifecycleAction(sprint.sprintId, 'start')}
                                                    disabled={actionLoading === sprint.sprintId}
                                                    className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-green-700 hover:bg-green-50 dark:text-green-400 dark:hover:bg-green-900/30"
                                                    title="Start Sprint"
                                                >
                                                    <Play size={12} /> Start
                                                </button>
                                            )}
                                            {sprint.status === SprintStatus.Active && (
                                                <>
                                                    <button
                                                        onClick={() => handleLifecycleAction(sprint.sprintId, 'complete')}
                                                        disabled={actionLoading === sprint.sprintId}
                                                        className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-blue-700 hover:bg-blue-50 dark:text-blue-400 dark:hover:bg-blue-900/30"
                                                        title="Complete Sprint"
                                                    >
                                                        <CheckCircle2 size={12} /> Complete
                                                    </button>
                                                    <button
                                                        onClick={() => handleLifecycleAction(sprint.sprintId, 'cancel')}
                                                        disabled={actionLoading === sprint.sprintId}
                                                        className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-red-700 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-900/30"
                                                        title="Cancel Sprint"
                                                    >
                                                        <XCircle size={12} /> Cancel
                                                    </button>
                                                </>
                                            )}
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            <Pagination
                page={page}
                pageSize={pageSize}
                totalCount={totalCount}
                onPageChange={setPage}
                onPageSizeChange={setPageSize}
            />

            <Modal open={createOpen} onClose={() => setCreateOpen(false)} title="Create Sprint">
                <SprintForm
                    projects={projects}
                    onSuccess={() => { setCreateOpen(false); fetchSprints(); }}
                />
            </Modal>
        </div>
    );
}
