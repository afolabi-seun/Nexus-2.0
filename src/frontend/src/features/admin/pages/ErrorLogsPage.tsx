import { useState, useEffect, useCallback } from 'react';
import { utilityApi } from '@/api/utilityApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Pagination } from '@/components/common/Pagination';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { useToast } from '@/components/common/Toast';
import type { ErrorLog } from '@/types/utility';
import { AlertTriangle, ChevronDown, ChevronRight } from 'lucide-react';

const severityColors: Record<string, string> = {
    Critical: 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
    Error: 'bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300',
    Warning: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900 dark:text-yellow-300',
    Info: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
};

function SeverityBadge({ severity }: { severity: string }) {
    const color = severityColors[severity] ?? 'bg-secondary text-secondary-foreground';
    return (
        <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${color}`}>
            {severity}
        </span>
    );
}

export function ErrorLogsPage() {
    const { addToast } = useToast();
    const [logs, setLogs] = useState<ErrorLog[]>([]);
    const [loading, setLoading] = useState(true);
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);
    const [totalCount, setTotalCount] = useState(0);
    const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());

    const [filterServiceName, setFilterServiceName] = useState('');
    const [filterErrorCode, setFilterErrorCode] = useState('');
    const [filterSeverity, setFilterSeverity] = useState('');
    const [filterDateFrom, setFilterDateFrom] = useState('');
    const [filterDateTo, setFilterDateTo] = useState('');

    const fetchLogs = useCallback(async () => {
        setLoading(true);
        try {
            const params: Record<string, unknown> = { page, pageSize };
            if (filterServiceName) params.serviceName = filterServiceName;
            if (filterErrorCode) params.errorCode = filterErrorCode;
            if (filterSeverity) params.severity = filterSeverity;
            if (filterDateFrom) params.dateFrom = filterDateFrom;
            if (filterDateTo) params.dateTo = filterDateTo;

            const result = await utilityApi.getErrorLogs(params as never);
            setLogs(result.data);
            setTotalCount(result.totalCount);
        } catch {
            addToast('error', 'Failed to load error logs');
        } finally {
            setLoading(false);
        }
    }, [page, pageSize, filterServiceName, filterErrorCode, filterSeverity, filterDateFrom, filterDateTo, addToast]);

    useEffect(() => { fetchLogs(); }, [fetchLogs]);

    const toggleRow = (id: string) => {
        setExpandedRows((prev) => {
            const next = new Set(prev);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });
    };

    const columns: Column<ErrorLog>[] = [
        {
            key: 'expand',
            header: '',
            render: (row) =>
                row.stackTrace ? (
                    <button
                        onClick={(e) => { e.stopPropagation(); toggleRow(row.errorLogId); }}
                        className="p-0.5 text-muted-foreground hover:text-foreground"
                        aria-label={expandedRows.has(row.errorLogId) ? 'Collapse stack trace' : 'Expand stack trace'}
                    >
                        {expandedRows.has(row.errorLogId) ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                    </button>
                ) : null,
        },
        { key: 'serviceName', header: 'Service' },
        { key: 'errorCode', header: 'Error Code' },
        {
            key: 'severity',
            header: 'Severity',
            render: (row) => <SeverityBadge severity={row.severity} />,
        },
        {
            key: 'message',
            header: 'Message',
            render: (row) => <span className="truncate max-w-xs block">{row.message}</span>,
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
                <AlertTriangle size={24} /> Error Logs
            </h1>

            {/* Filters */}
            <div className="flex flex-wrap gap-3">
                <select
                    value={filterServiceName}
                    onChange={(e) => { setFilterServiceName(e.target.value); setPage(1); }}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                >
                    <option value="">All Services</option>
                    <option value="WorkService">Work Service</option>
                    <option value="SecurityService">Security Service</option>
                    <option value="BillingService">Billing Service</option>
                    <option value="UtilityService">Utility Service</option>
                    <option value="ProfileService">Profile Service</option>
                </select>
                <input
                    type="text"
                    placeholder="Error Code"
                    value={filterErrorCode}
                    onChange={(e) => { setFilterErrorCode(e.target.value); setPage(1); }}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                />
                <select
                    value={filterSeverity}
                    onChange={(e) => { setFilterSeverity(e.target.value); setPage(1); }}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                >
                    <option value="">All Severities</option>
                    <option value="Critical">Critical</option>
                    <option value="Error">Error</option>
                    <option value="Warning">Warning</option>
                    <option value="Info">Info</option>
                </select>
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

            {loading ? (
                <SkeletonLoader variant="table" rows={5} columns={6} />
            ) : (
                <div className="space-y-0">
                    <DataTable
                        columns={columns}
                        data={logs}
                        keyExtractor={(row) => row.errorLogId}
                    />
                    {/* Expanded stack traces */}
                    {logs.filter((l) => expandedRows.has(l.errorLogId) && l.stackTrace).map((log) => (
                        <div key={`st-${log.errorLogId}`} className="border border-t-0 border-border bg-muted/30 px-4 py-3">
                            <pre className="whitespace-pre-wrap text-xs text-muted-foreground font-mono overflow-x-auto">
                                {log.stackTrace}
                            </pre>
                        </div>
                    ))}
                </div>
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
