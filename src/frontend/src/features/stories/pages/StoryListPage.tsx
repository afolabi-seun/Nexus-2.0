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
import { useDebounce } from '@/hooks/useDebounce';
import { StoryStatus, Priority } from '@/types/enums';
import type { StoryListItem, StoryFilters, ProjectListItem } from '@/types/work';
import type { Department, TeamMember } from '@/types/profile';
import { StoryForm } from '../components/StoryForm.js';
import { Plus, Search, X } from 'lucide-react';

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

    // Filter state
    const [filters, setFilters] = useState<StoryFilters>({});
    const [projects, setProjects] = useState<ProjectListItem[]>([]);
    const [departments, setDepartments] = useState<Department[]>([]);
    const [assigneeQuery, setAssigneeQuery] = useState('');
    const [assigneeResults, setAssigneeResults] = useState<TeamMember[]>([]);
    const [assigneeOpen, setAssigneeOpen] = useState(false);
    const [selectedAssigneeName, setSelectedAssigneeName] = useState('');
    const debouncedAssigneeQuery = useDebounce(assigneeQuery, 300);
    const [filtersVisible, setFiltersVisible] = useState(false);

    const canCreate = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead' || user?.roleName === 'Member';

    // Load filter options
    useEffect(() => {
        Promise.all([
            workApi.getProjects({ page: 1, pageSize: 100 }),
            profileApi.getDepartments(),
        ]).then(([projRes, depts]) => {
            setProjects(projRes.data);
            setDepartments(depts);
        }).catch(() => { });
    }, []);

    // Assignee search
    useEffect(() => {
        if (!debouncedAssigneeQuery.trim()) { setAssigneeResults([]); return; }
        profileApi.getTeamMembers({ page: 1, pageSize: 10 }).then((res) => {
            setAssigneeResults(
                res.data.filter((m) =>
                    `${m.firstName} ${m.lastName}`.toLowerCase().includes(debouncedAssigneeQuery.toLowerCase())
                )
            );
        }).catch(() => setAssigneeResults([]));
    }, [debouncedAssigneeQuery]);

    const fetchStories = useCallback(async () => {
        setLoading(true);
        try {
            const res = await workApi.getStories({ page, pageSize, sortBy, sortDirection, ...filters });
            setStories(res.data);
            setTotalCount(res.totalCount);
        } catch {
            addToast('error', 'Failed to load stories');
        } finally {
            setLoading(false);
        }
    }, [page, pageSize, sortBy, sortDirection, filters, addToast]);

    useEffect(() => { fetchStories(); }, [fetchStories]);

    const handleSort = (key: string) => {
        if (sortBy === key) {
            setSortDirection((d) => (d === 'asc' ? 'desc' : 'asc'));
        } else {
            setSortBy(key);
            setSortDirection('asc');
        }
    };

    const updateFilter = (key: keyof StoryFilters, value: unknown) => {
        setFilters((prev) => ({ ...prev, [key]: value || undefined }));
        setPage(1);
    };

    const toggleMultiFilter = (key: 'status' | 'priority', value: string) => {
        setFilters((prev) => {
            const current = (prev[key] as string[] | undefined) ?? [];
            const next = current.includes(value)
                ? current.filter((v) => v !== value)
                : [...current, value];
            return { ...prev, [key]: next.length > 0 ? next : undefined };
        });
        setPage(1);
    };

    const clearFilters = () => {
        setFilters({});
        setSelectedAssigneeName('');
        setAssigneeQuery('');
        setPage(1);
    };

    const hasActiveFilters = Object.values(filters).some((v) =>
        Array.isArray(v) ? v.length > 0 : v !== undefined
    );

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
                        onClick={() => setFiltersVisible((v) => !v)}
                        className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-2 text-sm font-medium text-foreground hover:bg-accent"
                    >
                        <Search size={14} />
                        Filters
                        {hasActiveFilters && (
                            <span className="ml-1 flex h-4 w-4 items-center justify-center rounded-full bg-primary text-[10px] text-primary-foreground">
                                !
                            </span>
                        )}
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

            {/* Filter Panel */}
            {filtersVisible && (
                <div className="rounded-lg border border-border bg-card p-4 space-y-3">
                    <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-card-foreground">Filters</span>
                        {hasActiveFilters && (
                            <button onClick={clearFilters} className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1">
                                <X size={12} /> Clear all
                            </button>
                        )}
                    </div>
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
                        {/* Project */}
                        <div>
                            <label className="mb-1 block text-xs text-muted-foreground">Project</label>
                            <select
                                value={filters.projectId ?? ''}
                                onChange={(e) => updateFilter('projectId', e.target.value)}
                                className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm text-foreground"
                            >
                                <option value="">All Projects</option>
                                {projects.map((p) => (
                                    <option key={p.projectId} value={p.projectId}>{p.name}</option>
                                ))}
                            </select>
                        </div>

                        {/* Status multi-select */}
                        <div>
                            <label className="mb-1 block text-xs text-muted-foreground">Status</label>
                            <div className="flex flex-wrap gap-1">
                                {Object.values(StoryStatus).map((s) => (
                                    <button
                                        key={s}
                                        type="button"
                                        onClick={() => toggleMultiFilter('status', s)}
                                        className={`rounded-full px-2 py-0.5 text-xs font-medium border ${filters.status?.includes(s)
                                            ? 'bg-primary text-primary-foreground border-primary'
                                            : 'bg-background text-muted-foreground border-input hover:bg-accent'
                                            }`}
                                    >
                                        {s}
                                    </button>
                                ))}
                            </div>
                        </div>

                        {/* Priority multi-select */}
                        <div>
                            <label className="mb-1 block text-xs text-muted-foreground">Priority</label>
                            <div className="flex flex-wrap gap-1">
                                {Object.values(Priority).map((p) => (
                                    <button
                                        key={p}
                                        type="button"
                                        onClick={() => toggleMultiFilter('priority', p)}
                                        className={`rounded-full px-2 py-0.5 text-xs font-medium border ${filters.priority?.includes(p)
                                            ? 'bg-primary text-primary-foreground border-primary'
                                            : 'bg-background text-muted-foreground border-input hover:bg-accent'
                                            }`}
                                    >
                                        {p}
                                    </button>
                                ))}
                            </div>
                        </div>

                        {/* Department */}
                        <div>
                            <label className="mb-1 block text-xs text-muted-foreground">Department</label>
                            <select
                                value={filters.departmentId ?? ''}
                                onChange={(e) => updateFilter('departmentId', e.target.value)}
                                className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm text-foreground"
                            >
                                <option value="">All Departments</option>
                                {departments.map((d) => (
                                    <option key={d.departmentId} value={d.departmentId}>{d.name}</option>
                                ))}
                            </select>
                        </div>

                        {/* Assignee (searchable) */}
                        <div className="relative">
                            <label className="mb-1 block text-xs text-muted-foreground">Assignee</label>
                            <input
                                type="text"
                                value={assigneeOpen ? assigneeQuery : selectedAssigneeName || assigneeQuery}
                                onChange={(e) => { setAssigneeQuery(e.target.value); setAssigneeOpen(true); }}
                                onFocus={() => setAssigneeOpen(true)}
                                placeholder="Search assignee..."
                                className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm text-foreground placeholder:text-muted-foreground"
                            />
                            {assigneeOpen && assigneeQuery.trim() && (
                                <ul className="absolute z-50 mt-1 max-h-40 w-full overflow-auto rounded-md border border-border bg-popover py-1 shadow-lg">
                                    {assigneeResults.length === 0 && (
                                        <li className="px-2 py-1 text-xs text-muted-foreground">No results</li>
                                    )}
                                    {assigneeResults.map((m) => (
                                        <li key={m.teamMemberId}>
                                            <button
                                                type="button"
                                                onClick={() => {
                                                    updateFilter('assigneeId', m.teamMemberId);
                                                    setSelectedAssigneeName(`${m.firstName} ${m.lastName}`);
                                                    setAssigneeQuery('');
                                                    setAssigneeOpen(false);
                                                }}
                                                className="w-full px-2 py-1 text-left text-sm hover:bg-accent text-popover-foreground"
                                            >
                                                {m.firstName} {m.lastName}
                                            </button>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </div>

                        {/* Date range */}
                        <div>
                            <label className="mb-1 block text-xs text-muted-foreground">From Date</label>
                            <input
                                type="date"
                                value={filters.dateFrom ?? ''}
                                onChange={(e) => updateFilter('dateFrom', e.target.value)}
                                className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm text-foreground"
                            />
                        </div>
                        <div>
                            <label className="mb-1 block text-xs text-muted-foreground">To Date</label>
                            <input
                                type="date"
                                value={filters.dateTo ?? ''}
                                onChange={(e) => updateFilter('dateTo', e.target.value)}
                                className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm text-foreground"
                            />
                        </div>
                    </div>
                </div>
            )}

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
