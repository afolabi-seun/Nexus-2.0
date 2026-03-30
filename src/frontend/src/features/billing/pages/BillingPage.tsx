import { useEffect, useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { billingApi } from '@/api/billingApi';
import { ApiError } from '@/types/api';
import type { SubscriptionDetailResponse } from '@/types/billing';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { SubscriptionOverview } from '../components/SubscriptionOverview';
import { PlanDetailsCard } from '../components/PlanDetailsCard';
import { UsageMeter } from '../components/UsageMeter';
import { SubscriptionActions } from '../components/SubscriptionActions';

type PageState =
    | { kind: 'loading' }
    | { kind: 'data'; detail: SubscriptionDetailResponse }
    | { kind: 'empty' }
    | { kind: 'error'; message: string };

export function BillingPage() {
    const [state, setState] = useState<PageState>({ kind: 'loading' });

    const fetchData = useCallback(async () => {
        setState({ kind: 'loading' });
        try {
            const detail = await billingApi.getCurrentSubscription();
            setState({ kind: 'data', detail });
        } catch (err) {
            if (err instanceof ApiError && err.errorCode === 'SUBSCRIPTION_NOT_FOUND') {
                setState({ kind: 'empty' });
            } else {
                setState({ kind: 'error', message: 'Failed to load billing information.' });
            }
        }
    }, []);

    useEffect(() => { fetchData(); }, [fetchData]);

    if (state.kind === 'loading') {
        return (
            <div className="space-y-6 p-6">
                <h1 className="text-2xl font-bold text-foreground">Billing</h1>
                <SkeletonLoader variant="form" />
            </div>
        );
    }

    if (state.kind === 'empty') {
        return (
            <div className="space-y-6 p-6">
                <h1 className="text-2xl font-bold text-foreground">Billing</h1>
                <div className="rounded-lg border border-border bg-card p-8 text-center">
                    <p className="mb-4 text-muted-foreground">No subscription found</p>
                    <Link
                        to="/billing/plans"
                        className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
                    >
                        Choose a Plan
                    </Link>
                </div>
            </div>
        );
    }

    if (state.kind === 'error') {
        return (
            <div className="space-y-6 p-6">
                <h1 className="text-2xl font-bold text-foreground">Billing</h1>
                <div className="rounded-lg border border-border bg-card p-8 text-center">
                    <p className="mb-4 text-muted-foreground">{state.message}</p>
                    <button
                        onClick={fetchData}
                        className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
                    >
                        Retry
                    </button>
                </div>
            </div>
        );
    }

    const { subscription, plan, usage } = state.detail;

    return (
        <div className="space-y-6 p-6">
            <h1 className="text-2xl font-bold text-foreground">Billing</h1>

            <div className="grid gap-6 lg:grid-cols-2">
                <SubscriptionOverview subscription={subscription} />
                <PlanDetailsCard plan={plan} />
            </div>

            <div className="rounded-lg border border-border bg-card p-6">
                <h3 className="mb-4 text-lg font-semibold text-card-foreground">Usage</h3>
                <div className="grid gap-6 md:grid-cols-3">
                    {usage.metrics.map((m) => (
                        <UsageMeter
                            key={m.metricName}
                            metricName={m.metricName}
                            currentValue={m.currentValue}
                            limit={m.limit}
                            percentUsed={m.percentUsed}
                        />
                    ))}
                </div>
            </div>

            <SubscriptionActions subscription={subscription} onCancelSuccess={fetchData} />
        </div>
    );
}
