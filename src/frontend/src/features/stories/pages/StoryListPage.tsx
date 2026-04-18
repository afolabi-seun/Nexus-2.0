import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { profileApi } from '@/api/profileApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Pagination } from '@/components/common/Pagination';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import { usePagination } from '@/hooks/usePagination';
import { useAuth } from '@/hooks/useAuth';
import { ListFilter } from '@/components/common/ListFilter';
import { useListFilters } from '@/hooks/useListFilters';
import { StoryStatus, Priority } from '@/types/enums';
import type { FilterConfig } from '@/types/filters';
import type { StoryListItem } from '@/types/work';
import { StoryForm } from '../components/StoryForm.js';
import { Plus, Download } from 'lucide-react';

const storyFilterConfigs: FilterConfig[] = [
    {
        key: 'projectId',
        label: 'Project',
        type: 'select',
        loadOptions: async () => {
            const res = await workApi.getProjects({ page: 1, pageSize: 100 });
            return res.data.map((p) => ({ value: p.projectId, label: p.name }));
        },
    },
    {
        key: 'status',
        label: 'Status',
        type: 'multi-select',
        options: Object.values(StoryStatus).map((s) => ({ value: s, label: s })),
    },
    {
        key: 'priority',
        label: 'Priority',
        type: 'multi-select',
        options: Object.values(Priority).map((p) => ({ value: p, label: p })),
    },
    {
        key: 'departmentId',
        label: 'Department',
        type: 'select',
        loadOptions: async () => {
            const res = await profileApi.getDepartments();
            return res.data.map((d) => ({ value: d.departmentId, label: d.name }));
        },
    },
    {
        key: 'assigneeId',
        label: 'Assignee',
        type: 'async-search',
        placeholder: 'Search assignee...',
        loadOptions: async (query) => {
            const res = await profileApi.getTeamMembers({ page: 1, pageSize: 10 });
            return res.data
                .filter((m) =>
                    `${m.firstName} ${m.lastName}`.toLowerCase().includes(query.toLowerCase())
                )
                .map((m) => ({ value: m.teamMemberId, label: `${m.firstName} ${m.lastName}` }));
        },
    },
    { key: 'dateFrom', label: 'From Date', type: 'date' },
    { key: 'dateTo', label: 'To Date', type: 'date' },
];

export function StoryListPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const { user } = useAuth();
    const { page, pageSize, setPage, setPageSize } = usePagination();

    const [stories, setStories] = useState<StoryListItem[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [loading, setLoading] = useState(true);
    const [sortBy, setSortBy] = useState<string | undefined>();
    const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
    const [createOpen, setCreateOpen] = useState(false);

    const canCreate = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead' || user?.roleName === 'Member';

    const { filterValues, updateFilter, clearFilters, hasActiveFilters, activeFilterCount } =
        useListFilters(storyFilterConfigs, { onPageReset: () => setPage(1) });

    const fetchStories = useCallback(async () => {
        setLoading(true);
        try {
            const res = await workApi.getStories({
                page,
                pageSize,
                sortBy,
                sortDirection,
                projectId: filterValues.projectId as string | undefined,
                status: filterValues.status as string[] | undefined,
                priority: filterValues.priority as string[] | undefined,
                departmentId: filterValues.departmentId as string | undefined,
                assigneeId: filterValues.assigneeId as string | undefined,
                dateFrom: filterValues.dateFrom as string | undefined,
                dateTo: filterValues.dateTo as string | undefined,
            });
            setStories(res.data);
            setTotalCount(res.totalCount);
        } catch {
            addToast('error', 'Failed to load stories');
        } finally {
            setLoading(false);
        }
    }, [page, pageSize, sortBy, sortDirection, filterValues, addToast]);

    useEffect(() => { fetchStories(); }, [fetchStories]);

    const handleSort = (key: string) => {
        if (sortBy === key) {
            setSortDirection((d) => (d === 'asc' ? 'desc' : 'asc'));
        } else {
            setSortBy(key);
            setSortDirection('asc');
        }
    };

    const columns: Column<StoryListItem>[] = [
        { key: 'storyKey', header: 'Key', sortable: true },
        { key: 'title', header: 'Title', sortable: true },
        {
            key: 'status', header: 'Status', sortable: true,
            render: (row) => <Badge variant="status" value={row.status} />,
        },
        {
            key: 'priority', header: 'Priority', sortable: true,
            render: (row) => <Badge variant="priority" value={row.priority} />,
        },
        {
            key: 'storyPoints', header: 'Points', sortable: true,
            render: (row) => <span>{row.storyPoints ?? '—'}</span>,
        },
        {
            key: 'assigneeName', header: 'Assignee',
            render: (row) => row.assigneeName ?? '—',
        },
        {
            key: 'sprintName', header: 'Sprint',
            render: (row) => row.sprintName ?? '—',
        },
        {
            key: 'projectName', header: 'Project', sortable: true,
        },
        {
            key: 'labels', header: 'Labels',
            render: (row) => (
                <div className="flex flex-wrap gap-1">
                    {row.labels.map((l) => (
                        <span
                            key={l.labelId}
                            className="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium text-white"
                            style={{ backgroundColor: l.color }}
                        >
                            {l.name}
                        </span>
                    ))}
                </div>
            ),
        },
    ];

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-semibold text-foreground">Stories</h1>
                <div className="flex items-center gap-2">
                    <button
                        onClick={async () => {
                            try {
                                const blob = await workApi.exportStoriesCsv({});
                                const url = URL.createObjectURL(blob);
                                const a = document.createElement('a');
                                a.href = url;
                                a.download = `stories_${new Date().toISOString().slice(0, 10)}.csv`;
                                a.click();
                                URL.revokeObjectURL(url);
                                addToast('success', 'Stories exported');
                            } catch {
                                addToast('error', 'Failed to export stories');
                            }
                        }}
                        className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-2 text-sm font-medium text-foreground hover:bg-accent"
                    >
                        <Download size={14} /> Export CSV
                    </button>
                    {canCreate && (
                        <button
                            onClick={() => setCreateOpen(true)}
                            className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                        >
                            <Plus size={16} />
                            Create Story
                        </button>
                    )}
                </div>
            </div>

            <ListFilter
                configs={storyFilterConfigs}
                values={filterValues}
                onUpdateFilter={updateFilter}
                onClearFilters={clearFilters}
                hasActiveFilters={hasActiveFilters}
                activeFilterCount={activeFilterCount}
            />

            <DataTable
                columns={columns}
                data={stories}
                loading={loading}
                sortBy={sortBy}
                sortDirection={sortDirection}
                onSort={handleSort}
                onRowClick={(row) => navigate(`/stories/${row.storyId}`)}
                keyExtractor={(row) => row.storyId}
            />

            <Pagination
                page={page}
                pageSize={pageSize}
                totalCount={totalCount}
                onPageChange={setPage}
                onPageSizeChange={setPageSize}
            />

            <Modal open={createOpen} onClose={() => setCreateOpen(false)} title="Create Story">
                <StoryForm
                    onSuccess={(story) => {
                        setCreateOpen(false);
                        navigate(`/stories/${story.storyId}`);
                    }}
                />
            </Modal>
        </div>
    );
}
