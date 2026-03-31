import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { CreditCard, RefreshCw } from 'lucide-react';
import { adminBillingApi } from '@/api/adminBillingApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Badge } from '@/components/common/Badge';
import { Pagination } from '@/components/common/Pagination';
import { useToast } from '@/components/common/Toast';
import { ListFilter } from '@/components/common/ListFilter';
import { useListFilters } from '@/hooks/useListFilters';
import { OverridePlanModal } from '@/features/admin/components/OverridePlanModal';
import { CancelSubscriptionModal } from '@/features/admin/components/CancelSubscriptionModal';
import type { FilterConfig } from '@/types/filters';
import type { AdminSubscriptionListItem, AdminPlanResponse } from '@/types/adminBilling';

const STATUSES = ['Active', 'Trialing', 'PastDue', 'Cancelled', 'Expired'] as const;

const filterConfigs: FilterConfig[] = [
    {
        key: 'status',
        label: 'Status',
        type: 'select',
        options: STATUSES.map((s) => ({ value: s, label: s })),
    },
    {
        key: 'search',
        label: 'Organization',
        type: 'text-search',
        placeholder: 'Search organizations…',
    },
];

export function PlatformAdminBillingPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();

    const [subscriptions, setSubscriptions] = useState<AdminSubscriptionListItem[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);
    const [loading, setLoading] = useState(true);

    // Summary counts
    const [statusCounts, setStatusCounts] = useState<Record<string, number>>({});

    // Plans for override modal
    const [plans, setPlans] = useState<AdminPlanResponse[]>([]);

    // Modal state
    const [overrideTarget, setOverrideTarget] = useState<AdminSubscriptionListItem | null>(null);
    const [cancelTarget, setCancelTarget] = useState<AdminSubscriptionListItem | null>(null);

    const { filterValues, updateFilter, clearFilters, hasActiveFilters, activeFilterCount } =
        useListFilters(filterConfigs, { onPageReset: () => setPage(1) });

    const fetchSubscriptions = useCallback(async () => {
        setLoading(true);
        try {
            const result = await adminBillingApi.getSubscriptions({
                status: filterValues.status as string | undefined,
                search: filterValues.search as string | undefined,
                page,
                pageSize,
            });
            setSubscriptions(result.items);
            setTotalCount(result.totalCount);
        } catch {
            addToast('error', 'Failed to load subscriptions');
        } finally {
            setLoading(false);
        }
    }, [filterValues, page, pageSize, addToast]);

    const fetchSummaryCounts = useCallback(async () => {
        try {
            // Fetch all subscriptions without filter to compute counts
            const all = await adminBillingApi.getSubscriptions({ pageSize: 1 });
            const counts: Record<string, number> = {};
            // We'll fetch per-status counts
            await Promise.all(
                STATUSES.map(async (s) => {
                    const r = await adminBillingApi.getSubscriptions({ status: s, pageSize: 1 });
                    counts[s] = r.totalCount;
                })
            );
            counts['Total'] = all.totalCount;
            setStatusCounts(counts);
        } catch {
            // non-critical
        }
    }, []);

    const fetchPlans = useCallback(async () => {
        try {
            const p = await adminBillingApi.getPlans();
            setPlans(p);
        } catch {
            // non-critical
        }
    }, []);

    useEffect(() => {
        fetchSubscriptions();
    }, [fetchSubscriptions]);

    useEffect(() => {
        fetchSummaryCounts();
        fetchPlans();
    }, [fetchSummaryCounts, fetchPlans]);

    const handleOverride = async (planId: string, reason?: string) => {
        if (!overrideTarget) return;
        await adminBillingApi.overrideSubscription(overrideTarget.organizationId, { planId, reason });
        addToast('success', 'Subscription overridden');
        fetchSubscriptions();
        fetchSummaryCounts();
    };

    const handleCancel = async (reason: string) => {
        if (!cancelTarget) return;
        await adminBillingApi.cancelSubscription(cancelTarget.organizationId, { reason });
        addToast('success', 'Subscription cancelled');
        fetchSubscriptions();
        fetchSummaryCounts();
    };

    const columns: Column<AdminSubscriptionListItem>[] = [
        { key: 'organizationName', header: 'Organization', sortable: true },
        { key: 'planName', header: 'Plan', sortable: true },
        {
            key: 'status',
            header: 'Status',
            render: (row) => <Badge variant="status" value={row.status} />,
        },
        {
            key: 'currentPeriodEnd',
            header: 'Period End',
            render: (row) => row.currentPeriodEnd ? new Date(row.currentPeriodEnd).toLocaleDateString() : '—',
        },
        {
            key: 'actions',
            header: 'Actions',
            render: (row) => (
                <div className="flex items-center gap-1">
                    <button
                        onClick={(e) => { e.stopPropagation(); setOverrideTarget(row); }}
                        className="rounded px-2 py-1 text-xs font-medium text-primary hover:bg-primary/10"
                        title="Override Plan"
                    >
                        Override
                    </button>
                    {(row.status === 'Active' || row.status === 'Trialing') && (
                        <button
                            onClick={(e) => { e.stopPropagation(); setCancelTarget(row); }}
                            className="rounded px-2 py-1 text-xs font-medium text-destructive hover:bg-destructive/10"
                            title="Cancel Subscription"
                        >
                            Cancel
                        </button>
                    )}
                </div>
            ),
        },
    ];

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <h1 className="flex items-center gap-2 text-2xl font-semibold text-foreground">
                    <CreditCard size={24} /> Billing Management
                </h1>
                <button onClick={() => { fetchSubscriptions(); fetchSummaryCounts(); }} className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-1.5 text-sm text-muted-foreground hover:bg-accent">
                    <RefreshCw size={14} /> Refresh
                </button>
            </div>

            {/* Summary Cards */}
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
                {(['Total', ...STATUSES] as const).map((s) => (
                    <div key={s} className="rounded-lg border border-border bg-card p-4">
                        <p className="text-xs font-medium text-muted-foreground">{s}</p>
                        <p className="mt-1 text-2xl font-semibold text-foreground">{statusCounts[s] ?? '—'}</p>
                    </div>
                ))}
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
                data={subscriptions}
                loading={loading}
                onRowClick={(row) => navigate(`/admin/billing/organizations/${row.organizationId}`)}
                keyExtractor={(row) => row.subscriptionId}
            />

            <Pagination
                page={page}
                pageSize={pageSize}
                totalCount={totalCount}
                onPageChange={setPage}
                onPageSizeChange={(s) => { setPageSize(s); setPage(1); }}
            />

            <OverridePlanModal
                open={!!overrideTarget}
                onClose={() => setOverrideTarget(null)}
                onSubmit={handleOverride}
                plans={plans}
                currentPlanId={overrideTarget?.planId}
            />

            <CancelSubscriptionModal
                open={!!cancelTarget}
                onClose={() => setCancelTarget(null)}
                onSubmit={handleCancel}
                organizationName={cancelTarget?.organizationName}
            />
        </div>
    );
}
