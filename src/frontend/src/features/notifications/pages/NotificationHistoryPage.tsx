import { useState, useEffect, useCallback } from 'react';
import { utilityApi } from '@/api/utilityApi';
import { useToast } from '@/components/common/Toast';
import { Pagination } from '@/components/common/Pagination';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import type { NotificationLog } from '@/types/utility';
import { Bell, Mail, Smartphone, Monitor, CheckCircle2, Clock, AlertCircle } from 'lucide-react';

const channelIcons: Record<string, React.ReactNode> = {
    Email: <Mail size={14} />,
    Push: <Smartphone size={14} />,
    InApp: <Monitor size={14} />,
};

const statusIcons: Record<string, React.ReactNode> = {
    Sent: <CheckCircle2 size={14} className="text-green-500" />,
    Pending: <Clock size={14} className="text-yellow-500" />,
    Failed: <AlertCircle size={14} className="text-red-500" />,
};

export function NotificationHistoryPage() {
    const { addToast } = useToast();
    const [logs, setLogs] = useState<NotificationLog[]>([]);
    const [loading, setLoading] = useState(true);
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);
    const [totalCount, setTotalCount] = useState(0);
    const [filterType, setFilterType] = useState('');
    const [filterChannel, setFilterChannel] = useState('');
    const [filterStatus, setFilterStatus] = useState('');

    const fetchLogs = useCallback(async () => {
        setLoading(true);
        try {
            const params: Record<string, unknown> = { page, pageSize };
            if (filterType) params.notificationType = filterType;
            if (filterChannel) params.channel = filterChannel;
            if (filterStatus) params.status = filterStatus;
            const result = await utilityApi.getNotificationLogs(params as never);
            setLogs(result.data);
            setTotalCount(result.totalCount);
        } catch {
            addToast('error', 'Failed to load notification history');
        } finally {
            setLoading(false);
        }
    }, [page, pageSize, filterType, filterChannel, filterStatus, addToast]);

    useEffect(() => { fetchLogs(); }, [fetchLogs]);

    return (
        <div className="space-y-6">
            <h1 className="flex items-center gap-2 text-2xl font-semibold text-foreground">
                <Bell size={24} /> Notification History
            </h1>

            {/* Filters */}
            <div className="flex flex-wrap gap-3">
                <select value={filterType} onChange={(e) => { setFilterType(e.target.value); setPage(1); }} className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground">
                    <option value="">All Types</option>
                    <option value="StoryAssigned">Story Assigned</option>
                    <option value="TaskAssigned">Task Assigned</option>
                    <option value="SprintStarted">Sprint Started</option>
                    <option value="SprintEnded">Sprint Ended</option>
                    <option value="MentionedInComment">Mentioned in Comment</option>
                    <option value="StoryStatusChanged">Story Status Changed</option>
                    <option value="TaskStatusChanged">Task Status Changed</option>
                    <option value="DueDateApproaching">Due Date Approaching</option>
                </select>
                <select value={filterChannel} onChange={(e) => { setFilterChannel(e.target.value); setPage(1); }} className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground">
                    <option value="">All Channels</option>
                    <option value="Email">Email</option>
                    <option value="Push">Push</option>
                    <option value="InApp">In-App</option>
                </select>
                <select value={filterStatus} onChange={(e) => { setFilterStatus(e.target.value); setPage(1); }} className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground">
                    <option value="">All Statuses</option>
                    <option value="Sent">Sent</option>
                    <option value="Pending">Pending</option>
                    <option value="Failed">Failed</option>
                </select>
            </div>

            {loading ? (
                <SkeletonLoader variant="table" rows={5} columns={5} />
            ) : logs.length === 0 ? (
                <p className="py-8 text-center text-muted-foreground">No notifications found</p>
            ) : (
                <div className="space-y-2">
                    {logs.map((log) => (
                        <div key={log.notificationLogId} className="flex items-start gap-3 rounded-md border border-border px-4 py-3">
                            <div className="mt-0.5 text-muted-foreground">
                                {channelIcons[log.channel] ?? <Bell size={14} />}
                            </div>
                            <div className="min-w-0 flex-1">
                                <p className="text-sm font-medium text-foreground">{log.subject}</p>
                                <div className="mt-1 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                                    <span className="rounded-full bg-muted px-2 py-0.5">{log.notificationType}</span>
                                    <span>{log.channel}</span>
                                    <span>{new Date(log.dateCreated).toLocaleString()}</span>
                                </div>
                            </div>
                            <div className="shrink-0">{statusIcons[log.status] ?? <span className="text-xs text-muted-foreground">{log.status}</span>}</div>
                        </div>
                    ))}
                </div>
            )}

            <Pagination page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={setPage} onPageSizeChange={(s) => { setPageSize(s); setPage(1); }} />
        </div>
    );
}
