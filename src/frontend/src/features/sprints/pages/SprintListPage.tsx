import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Modal } from '@/components/common/Modal';
import { Pagination } from '@/components/common/Pagination';
import { useToast } from '@/components/common/Toast';
import { usePagination } from '@/hooks/usePagination';
import { useAuth } from '@/hooks/useAuth';
import { ListFilter } from '@/components/common/ListFilter';
import { useListFilters } from '@/hooks/useListFilters';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { SprintStatus } from '@/types/enums';
import type { FilterConfig } from '@/types/filters';
import type { SprintListItem, ProjectListItem } from '@/types/work';
import { SprintForm } from '../components/SprintForm.js';
import { Plus, Play, CheckCircle2, XCircle } from 'lucide-react';
import { PageHeader } from '@/components/common/PageHeader';

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
];

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
    const [actionLoading, setActionLoading] = useState<string | null>(null);

    const canCreate = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead';

    const { filterValues, updateFilter, clearFilters, hasActiveFilters, activeFilterCount } =
        useListFilters(filterConfigs, { onPageReset: () => setPage(1) });

    const fetchSprints = useCallback(async () => {
        setLoading(true);
        try {
            const res = await workApi.getSprints({
                page,
                pageSize,
                projectId: filterValues.projectId as string | undefined,
            });
            setSprints(res.data);
            setTotalCount(res.totalCount);
        } catch {
            addToast('error', 'Failed to load sprints');
        } finally {
            setLoading(false);
        }
    }, [page, pageSize, filterValues, addToast]);

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

    const columns: Column<SprintListItem>[] = [
        { key: 'name', header: 'Sprint Name', sortable: true },
        { key: 'projectName', header: 'Project', sortable: true },
        {
            key: 'status',
            header: 'Status',
            render: (row) => <Badge variant="status" value={row.status} />,
        },
        {
            key: 'startDate',
            header: 'Dates',
            render: (row) =>
                `${new Date(row.startDate).toLocaleDateString()} – ${new Date(row.endDate).toLocaleDateString()}`,
        },
        { key: 'storyCount', header: 'Stories', sortable: true },
        {
            key: 'velocity',
            header: 'Velocity',
            render: (row) => String(row.velocity ?? '—'),
        },
        {
            key: 'actions',
            header: 'Actions',
            render: (row) => (
                <div className="flex gap-1" onClick={(e) => e.stopPropagation()}>
                    {row.status === SprintStatus.Planning && (
                        <button
                            onClick={() => handleLifecycleAction(row.sprintId, 'start')}
                            disabled={actionLoading === row.sprintId}
                            className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-green-700 hover:bg-green-50 dark:text-green-400 dark:hover:bg-green-900/30"
                            title="Start Sprint"
                        >
                            <Play size={12} /> Start
                        </button>
                    )}
                    {row.status === SprintStatus.Active && (
                        <>
                            <button
                                onClick={() => handleLifecycleAction(row.sprintId, 'complete')}
                                disabled={actionLoading === row.sprintId}
                                className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-blue-700 hover:bg-blue-50 dark:text-blue-400 dark:hover:bg-blue-900/30"
                                title="Complete Sprint"
                            >
                                <CheckCircle2 size={12} /> Complete
                            </button>
                            <button
                                onClick={() => handleLifecycleAction(row.sprintId, 'cancel')}
                                disabled={actionLoading === row.sprintId}
                                className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-red-700 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-900/30"
                                title="Cancel Sprint"
                            >
                                <XCircle size={12} /> Cancel
                            </button>
                        </>
                    )}
                </div>
            ),
        },
    ];

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <PageHeader title="Sprints" description="Time-boxed iterations for delivering stories. Plan, start, and complete sprints here." dismissKey="sprints" />
                {canCreate && (
                    <button
                        onClick={() => setCreateOpen(true)}
                        className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        <Plus size={16} /> Create Sprint
                    </button>
                )}
            </div>

            <ListFilter
                configs={filterConfigs}
                values={filterValues}
                onUpdateFilter={updateFilter}
                onClearFilters={clearFilters}
                hasActiveFilters={hasActiveFilters}
                activeFilterCount={activeFilterCount}
            />

            <DataTable
                columns={columns}
                data={sprints}
                loading={loading}
                onRowClick={(row) => navigate(`/sprints/${row.sprintId}`)}
                keyExtractor={(row) => row.sprintId}
                    emptyMessage="No sprints yet. Sprints are time-boxed iterations for delivering stories."
            />

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
