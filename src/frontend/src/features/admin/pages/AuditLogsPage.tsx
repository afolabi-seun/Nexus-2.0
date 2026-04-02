import { useState, useEffect, useCallback } from 'react';
import { utilityApi } from '@/api/utilityApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Pagination } from '@/components/common/Pagination';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { useToast } from '@/components/common/Toast';
import type { AuditLog } from '@/types/utility';
import { FileText } from 'lucide-react';

export function AuditLogsPage() {
    const { addToast } = useToast();
    const [logs, setLogs] = useState<AuditLog[]>([]);
    const [loading, setLoading] = useState(true);
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);
    const [totalCount, setTotalCount] = useState(0);
    const [isArchive, setIsArchive] = useState(false);

    const [filterAction, setFilterAction] = useState('');
    const [filterEntityType, setFilterEntityType] = useState('');
    const [filterActorId, setFilterActorId] = useState('');
    const [filterDateFrom, setFilterDateFrom] = useState('');
    const [filterDateTo, setFilterDateTo] = useState('');

    const fetchLogs = useCallback(async () => {
        setLoading(true);
        try {
            const params: Record<string, unknown> = { page, pageSize };
            if (filterAction) params.action = filterAction;
            if (filterEntityType) params.entityType = filterEntityType;
            if (filterActorId) params.actorId = filterActorId;
            if (filterDateFrom) params.dateFrom = filterDateFrom;
            if (filterDateTo) params.dateTo = filterDateTo;

            const result = isArchive
                ? await utilityApi.getArchivedAuditLogs(params as never)
                : await utilityApi.getAuditLogs(params as never);
            setLogs(result.data);
            setTotalCount(result.totalCount);
        } catch {
            addToast('error', 'Failed to load audit logs');
        } finally {
            setLoading(false);
        }
    }, [page, pageSize, isArchive, filterAction, filterEntityType, filterActorId, filterDateFrom, filterDateTo, addToast]);

    useEffect(() => { fetchLogs(); }, [fetchLogs]);

    const columns: Column<AuditLog>[] = [
        { key: 'action', header: 'Action' },
        { key: 'entityType', header: 'Entity Type' },
        { key: 'entityId', header: 'Entity ID' },
        { key: 'actorName', header: 'Actor' },
        { key: 'ipAddress', header: 'IP Address', render: (row) => row.ipAddress ?? '—' },
        {
            key: 'details',
            header: 'Details',
            render: (row) => (
                <span className="truncate max-w-xs block">{row.details ?? '—'}</span>
            ),
        },
        {
            key: 'dateCreated',
            header: 'Date',
            render: (row) => new Date(row.dateCreated).toLocaleString(),
        },
    ];

    return (
        <div className="space-y-6">
            <h1 className="flex items-center gap-2 text-2xl font-semibold text-foreground">
                <FileText size={24} /> Audit Logs
            </h1>

            {/* Filters */}
            <div className="flex flex-wrap gap-3">
                <select
                    value={filterAction}
                    onChange={(e) => { setFilterAction(e.target.value); setPage(1); }}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                >
                    <option value="">All Actions</option>
                    <option value="Create">Create</option>
                    <option value="Update">Update</option>
                    <option value="Delete">Delete</option>
                    <option value="Login">Login</option>
                    <option value="Logout">Logout</option>
                </select>
                <select
                    value={filterEntityType}
                    onChange={(e) => { setFilterEntityType(e.target.value); setPage(1); }}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                >
                    <option value="">All Entity Types</option>
                    <option value="Story">Story</option>
                    <option value="Task">Task</option>
                    <option value="Sprint">Sprint</option>
                    <option value="Project">Project</option>
                    <option value="Member">Member</option>
                </select>
                <input
                    type="text"
                    placeholder="Actor ID"
                    value={filterActorId}
                    onChange={(e) => { setFilterActorId(e.target.value); setPage(1); }}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                />
                <input
                    type="date"
                    value={filterDateFrom}
                    onChange={(e) => { setFilterDateFrom(e.target.value); setPage(1); }}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                    aria-label="Date from"
                />
                <input
                    type="date"
                    value={filterDateTo}
                    onChange={(e) => { setFilterDateTo(e.target.value); setPage(1); }}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                    aria-label="Date to"
                />
            </div>

            {/* Archive toggle */}
            <div className="flex gap-1 rounded-md border border-input p-0.5 w-fit">
                <button
                    onClick={() => { setIsArchive(false); setPage(1); }}
                    className={`rounded px-3 py-1.5 text-sm font-medium ${!isArchive ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent'}`}
                >
                    Live
                </button>
                <button
                    onClick={() => { setIsArchive(true); setPage(1); }}
                    className={`rounded px-3 py-1.5 text-sm font-medium ${isArchive ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent'}`}
                >
                    Archived
                </button>
            </div>

            {loading ? (
                <SkeletonLoader variant="table" rows={5} columns={7} />
            ) : (
                <DataTable
                    columns={columns}
                    data={logs}
                    keyExtractor={(row) => row.auditLogId}
                />
            )}

            <Pagination
                page={page}
                pageSize={pageSize}
                totalCount={totalCount}
                onPageChange={setPage}
                onPageSizeChange={(s) => { setPageSize(s); setPage(1); }}
            />
        </div>
    );
}
