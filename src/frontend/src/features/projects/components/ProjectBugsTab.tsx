import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Badge } from '@/components/common/Badge';
import { EmptyState } from '@/components/common/EmptyState';
import { Pagination } from '@/components/common/Pagination';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import type { StoryListItem } from '@/types/work';
import { Bug } from 'lucide-react';

interface ProjectBugsTabProps {
    projectId: string;
}

export function ProjectBugsTab({ projectId }: ProjectBugsTabProps) {
    const navigate = useNavigate();
    const [bugs, setBugs] = useState<StoryListItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [totalCount, setTotalCount] = useState(0);
    const [page, setPage] = useState(1);
    const [statusFilter, setStatusFilter] = useState<string>('');
    const [priorityFilter, setPriorityFilter] = useState<string>('');
    const pageSize = 20;

    const fetchBugs = useCallback(async () => {
        setLoading(true);
        try {
            const res = await workApi.getStories({
                projectId,
                storyType: 'Bug',
                status: statusFilter || undefined,
                priority: priorityFilter || undefined,
                page,
                pageSize,
            });
            setBugs(res.data ?? []);
            setTotalCount(res.totalCount ?? 0);
        } catch {
            setBugs([]);
        } finally {
            setLoading(false);
        }
    }, [projectId, page, statusFilter, priorityFilter]);

    useEffect(() => { fetchBugs(); }, [fetchBugs]);

    const openBugs = bugs.filter(b => !['Done', 'Closed'].includes(b.status));
    const closedBugs = bugs.filter(b => ['Done', 'Closed'].includes(b.status));

    const columns: Column<StoryListItem>[] = [
        {
            key: 'storyKey',
            header: 'Key',
            render: (b) => (
                <button
                    onClick={() => navigate(`/stories/${b.storyId}`)}
                    className="font-mono text-xs text-primary hover:underline"
                >
                    {b.storyKey}
                </button>
            ),
        },
        { key: 'title', header: 'Title', render: (b) => <span className="text-sm">{b.title}</span> },
        {
            key: 'priority',
            header: 'Priority',
            render: (b) => <Badge variant="priority" value={b.priority} />,
        },
        {
            key: 'status',
            header: 'Status',
            render: (b) => <Badge variant="status" value={b.status} />,
        },
        {
            key: 'assigneeName',
            header: 'Assignee',
            render: (b) => <span className="text-sm text-muted-foreground">{b.assigneeName ?? 'Unassigned'}</span>,
        },
        {
            key: 'dateCreated',
            header: 'Reported',
            render: (b) => <span className="text-xs text-muted-foreground">{new Date(b.dateCreated).toLocaleDateString()}</span>,
        },
    ];

    if (loading && bugs.length === 0) return <SkeletonLoader variant="table" rows={6} />;

    return (
        <div className="space-y-4">
            {/* Summary cards */}
            <div className="grid grid-cols-3 gap-3">
                <div className="rounded-lg border border-border bg-card p-3">
                    <p className="text-xs text-muted-foreground">Total Bugs</p>
                    <p className="mt-1 text-2xl font-semibold text-foreground">{totalCount}</p>
                </div>
                <div className="rounded-lg border border-border bg-card p-3">
                    <p className="text-xs text-muted-foreground">Open</p>
                    <p className="mt-1 text-2xl font-semibold text-yellow-600">{openBugs.length}</p>
                </div>
                <div className="rounded-lg border border-border bg-card p-3">
                    <p className="text-xs text-muted-foreground">Closed</p>
                    <p className="mt-1 text-2xl font-semibold text-green-600">{closedBugs.length}</p>
                </div>
            </div>

            {/* Filters */}
            <div className="flex gap-2">
                <select
                    value={statusFilter}
                    onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
                    className="rounded-md border border-input bg-background px-2 py-1 text-sm"
                >
                    <option value="">All Statuses</option>
                    <option value="Backlog">Backlog</option>
                    <option value="Ready">Ready</option>
                    <option value="InProgress">In Progress</option>
                    <option value="InReview">In Review</option>
                    <option value="QA">QA</option>
                    <option value="Done">Done</option>
                    <option value="Closed">Closed</option>
                </select>
                <select
                    value={priorityFilter}
                    onChange={(e) => { setPriorityFilter(e.target.value); setPage(1); }}
                    className="rounded-md border border-input bg-background px-2 py-1 text-sm"
                >
                    <option value="">All Priorities</option>
                    <option value="Critical">Critical</option>
                    <option value="High">High</option>
                    <option value="Medium">Medium</option>
                    <option value="Low">Low</option>
                </select>
            </div>

            {/* Bug list */}
            {bugs.length === 0 && !loading ? (
                <EmptyState
                    icon={<Bug className="h-10 w-10 text-muted-foreground" />}
                    title="No bugs reported"
                    description="Bugs will appear here when stories are created with type 'Bug'. Use the story form to report a bug."
                />
            ) : (
                <>
                    <DataTable<StoryListItem>
                        columns={columns}
                        data={bugs}
                        keyExtractor={(b) => b.storyId}
                        emptyMessage="No bugs match the current filters."
                    />
                    <Pagination
                        page={page}
                        pageSize={pageSize}
                        totalCount={totalCount}
                        onPageChange={setPage}
                        onPageSizeChange={() => { setPage(1); }}
                    />
                </>
            )}
        </div>
    );
}
