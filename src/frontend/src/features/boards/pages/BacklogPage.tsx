import { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { ListFilter } from '@/components/common/ListFilter';
import { useListFilters } from '@/hooks/useListFilters';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { SprintStatus, Priority } from '@/types/enums';
import type { FilterConfig } from '@/types/filters';
import type { Backlog, BacklogItem, SprintListItem } from '@/types/work';
import { Plus } from 'lucide-react';

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
    {
        key: 'priority',
        label: 'Priority',
        type: 'multi-select',
        options: Object.values(Priority).map((p) => ({ value: p, label: p })),
    },
];

export function BacklogPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const [backlog, setBacklog] = useState<Backlog | null>(null);
    const [loading, setLoading] = useState(true);
    const [addToSprintOpen, setAddToSprintOpen] = useState(false);
    const [selectedStory, setSelectedStory] = useState<BacklogItem | null>(null);
    const [sprints, setSprints] = useState<SprintListItem[]>([]);
    const [selectedSprintId, setSelectedSprintId] = useState('');
    const [adding, setAdding] = useState(false);

    const { filterValues, updateFilter, clearFilters, hasActiveFilters, activeFilterCount } =
        useListFilters(filterConfigs, { syncToUrl: true });

    const apiFilters = useMemo(() => ({
        projectId: filterValues.projectId as string | undefined,
        priority: Array.isArray(filterValues.priority)
            ? filterValues.priority[0]
            : (filterValues.priority as string | undefined),
    }), [filterValues]);

    const fetchBacklog = useCallback(async () => {
        setLoading(true);
        try {
            const data = await workApi.getBacklog(apiFilters);
            setBacklog(data);
        } catch {
            addToast('error', 'Failed to load backlog');
        } finally {
            setLoading(false);
        }
    }, [apiFilters, addToast]);

    useEffect(() => { fetchBacklog(); }, [fetchBacklog]);

    const openAddToSprint = async (story: BacklogItem) => {
        setSelectedStory(story);
        try {
            const res = await workApi.getSprints({ status: SprintStatus.Planning, page: 1, pageSize: 50 });
            setSprints(res.data);
        } catch {
            setSprints([]);
        }
        setAddToSprintOpen(true);
    };

    const handleAddToSprint = async () => {
        if (!selectedStory || !selectedSprintId) return;
        setAdding(true);
        try {
            await workApi.addStoryToSprint(selectedSprintId, { storyId: selectedStory.storyId });
            addToast('success', `Added "${selectedStory.storyKey}" to sprint`);
            setAddToSprintOpen(false);
            setSelectedSprintId('');
            fetchBacklog();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', 'Failed to add story to sprint');
            }
        } finally {
            setAdding(false);
        }
    };

    if (loading) return <SkeletonLoader variant="table" rows={8} columns={7} />;

    const backlogColumns: Column<BacklogItem>[] = [
        { key: 'storyKey', header: 'Key' },
        { key: 'title', header: 'Title', render: (row) => <span className="truncate max-w-xs block">{row.title}</span> },
        { key: 'priority', header: 'Priority', render: (row) => <Badge variant="priority" value={row.priority} /> },
        { key: 'storyPoints', header: 'Points', render: (row) => String(row.storyPoints ?? '—') },
        { key: 'assigneeName', header: 'Assignee', render: (row) => row.assigneeName ?? 'Unassigned' },
        {
            key: 'labels', header: 'Labels', render: (row) => (
                <div className="flex gap-1">
                    {row.labels.map((l) => (
                        <span key={l.labelId} className="h-3 w-3 rounded-full" style={{ backgroundColor: l.color }} title={l.name} />
                    ))}
                </div>
            ),
        },
        { key: 'dateCreated', header: 'Created', render: (row) => new Date(row.dateCreated).toLocaleDateString() },
        {
            key: 'actions', header: 'Actions', render: (row) => (
                <div onClick={(e) => e.stopPropagation()}>
                    <button
                        onClick={() => openAddToSprint(row)}
                        className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-primary hover:bg-primary/10"
                        title="Add to Sprint"
                    >
                        <Plus size={12} /> Sprint
                    </button>
                </div>
            ),
        },
    ];

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-semibold text-foreground">Backlog</h1>
                    {backlog && (
                        <p className="text-sm text-muted-foreground">
                            {backlog.totalStories} stories · {backlog.totalPoints} points
                        </p>
                    )}
                </div>
            </div>

            <ListFilter
                configs={filterConfigs}
                values={filterValues}
                onUpdateFilter={updateFilter}
                onClearFilters={clearFilters}
                hasActiveFilters={hasActiveFilters}
                activeFilterCount={activeFilterCount}
                enableSavedFilters
            />

            {!backlog || backlog.items.length === 0 ? (
                <div className="py-12 text-center text-muted-foreground">No stories in backlog</div>
            ) : (
                <DataTable
                    columns={backlogColumns}
                    data={backlog.items}
                    onRowClick={(row) => navigate(`/stories/${row.storyId}`)}
                    keyExtractor={(row) => row.storyId}
                />
            )}

            {/* Add to Sprint Modal */}
            <Modal
                open={addToSprintOpen}
                onClose={() => { setAddToSprintOpen(false); setSelectedSprintId(''); }}
                title={`Add "${selectedStory?.storyKey}" to Sprint`}
            >
                <div className="space-y-4">
                    {sprints.length === 0 ? (
                        <p className="text-sm text-muted-foreground">No sprints in planning status available.</p>
                    ) : (
                        <>
                            <select
                                value={selectedSprintId}
                                onChange={(e) => setSelectedSprintId(e.target.value)}
                                className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground"
                            >
                                <option value="">Select a sprint</option>
                                {sprints.map((s) => (
                                    <option key={s.sprintId} value={s.sprintId}>
                                        {s.name} ({s.projectName})
                                    </option>
                                ))}
                            </select>
                            <div className="flex justify-end gap-2">
                                <button
                                    onClick={() => { setAddToSprintOpen(false); setSelectedSprintId(''); }}
                                    className="rounded-md border border-input px-3 py-2 text-sm font-medium text-foreground hover:bg-accent"
                                >
                                    Cancel
                                </button>
                                <button
                                    onClick={handleAddToSprint}
                                    disabled={!selectedSprintId || adding}
                                    className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                                >
                                    {adding ? 'Adding...' : 'Add to Sprint'}
                                </button>
                            </div>
                        </>
                    )}
                </div>
            </Modal>
        </div>
    );
}
