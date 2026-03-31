import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { adminBillingApi } from '@/api/adminBillingApi';
import { Badge } from '@/components/common/Badge';
import { useToast } from '@/components/common/Toast';
import { OverridePlanModal } from '@/features/admin/components/OverridePlanModal';
import { CancelSubscriptionModal } from '@/features/admin/components/CancelSubscriptionModal';
import type { AdminOrganizationBillingResponse, AdminPlanResponse } from '@/types/adminBilling';

export function PlatformAdminOrgBillingDetailPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { addToast } = useToast();

    const [billing, setBilling] = useState<AdminOrganizationBillingResponse | null>(null);
    const [plans, setPlans] = useState<AdminPlanResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [overrideOpen, setOverrideOpen] = useState(false);
    const [cancelOpen, setCancelOpen] = useState(false);

    const fetchBilling = useCallback(async () => {
        if (!id) return;
        setLoading(true);
        try {
            const data = await adminBillingApi.getOrganizationBilling(id);
            setBilling(data);
        } catch {
            addToast('error', 'Failed to load billing details');
        } finally {
            setLoading(false);
        }
    }, [id, addToast]);

    const fetchPlans = useCallback(async () => {
        try {
            setPlans(await adminBillingApi.getPlans());
        } catch { /* non-critical */ }
    }, []);

    useEffect(() => { fetchBilling(); fetchPlans(); }, [fetchBilling, fetchPlans]);

    const handleOverride = async (planId: string, reason?: string) => {
        if (!id) return;
        await adminBillingApi.overrideSubscription(id, { planId, reason });
        addToast('success', 'Subscription overridden');
        fetchBilling();
    };

    const handleCancel = async (reason: string) => {
        if (!id) return;
        await adminBillingApi.cancelSubscription(id, { reason });
        addToast('success', 'Subscription cancelled');
        fetchBilling();
    };

    if (loading) {
        return (
            <div className="flex items-center justify-center py-20">
                <p className="text-muted-foreground">Loading billing details…</p>
            </div>
        );
    }

    if (!billing) {
        return (
            <div className="space-y-4">
                <button onClick={() => navigate('/admin/billing')} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
                    <ArrowLeft size={16} /> Back to Billing
                </button>
                <p className="text-muted-foreground">No billing data found for this organization.</p>
            </div>
        );
    }

    const { subscription, plan, usage } = billing;
    const isActive = subscription.status === 'Active' || subscription.status === 'Trialing';

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                    <button onClick={() => navigate('/admin/billing')} className="rounded-md p-1.5 text-muted-foreground hover:bg-accent hover:text-accent-foreground" aria-label="Back">
                        <ArrowLeft size={20} />
                    </button>
                    <h1 className="text-2xl font-semibold text-foreground">Organization Billing Detail</h1>
                </div>
                <div className="flex items-center gap-2">
                    <button onClick={() => setOverrideOpen(true)} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90">
                        Override Plan
                    </button>
                    {isActive && (
                        <button onClick={() => setCancelOpen(true)} className="rounded-md bg-destructive px-4 py-2 text-sm font-medium text-destructive-foreground hover:bg-destructive/90">
                            Cancel Subscription
                        </button>
                    )}
                </div>
            </div>

            {/* Subscription Info */}
            <div className="rounded-lg border border-border bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-card-foreground">Subscription</h2>
                <dl className="grid grid-cols-2 gap-x-8 gap-y-3 text-sm sm:grid-cols-3">
                    <div>
                        <dt className="text-muted-foreground">Status</dt>
                        <dd className="mt-0.5"><Badge variant="status" value={subscription.status} /></dd>
                    </div>
                    <div>
                        <dt className="text-muted-foreground">Plan</dt>
                        <dd className="mt-0.5 font-medium text-foreground">{subscription.planName} ({subscription.planCode})</dd>
                    </div>
                    <div>
                        <dt className="text-muted-foreground">Period Start</dt>
                        <dd className="mt-0.5 text-foreground">{new Date(subscription.currentPeriodStart).toLocaleDateString()}</dd>
                    </div>
                    <div>
                        <dt className="text-muted-foreground">Period End</dt>
                        <dd className="mt-0.5 text-foreground">{subscription.currentPeriodEnd ? new Date(subscription.currentPeriodEnd).toLocaleDateString() : '—'}</dd>
                    </div>
                    {subscription.trialEndDate && (
                        <div>
                            <dt className="text-muted-foreground">Trial End</dt>
                            <dd className="mt-0.5 text-foreground">{new Date(subscription.trialEndDate).toLocaleDateString()}</dd>
                        </div>
                    )}
                    {subscription.cancelledAt && (
                        <div>
                            <dt className="text-muted-foreground">Cancelled At</dt>
                            <dd className="mt-0.5 text-foreground">{new Date(subscription.cancelledAt).toLocaleDateString()}</dd>
                        </div>
                    )}
                </dl>
            </div>

            {/* Plan Details */}
            <div className="rounded-lg border border-border bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-card-foreground">Plan Details</h2>
                <dl className="grid grid-cols-2 gap-x-8 gap-y-3 text-sm sm:grid-cols-3">
                    <div>
                        <dt className="text-muted-foreground">Tier Level</dt>
                        <dd className="mt-0.5 text-foreground">{plan.tierLevel}</dd>
                    </div>
                    <div>
                        <dt className="text-muted-foreground">Monthly Price</dt>
                        <dd className="mt-0.5 text-foreground">${plan.priceMonthly}</dd>
                    </div>
                    <div>
                        <dt className="text-muted-foreground">Yearly Price</dt>
                        <dd className="mt-0.5 text-foreground">${plan.priceYearly}</dd>
                    </div>
                    <div>
                        <dt className="text-muted-foreground">Max Team Members</dt>
                        <dd className="mt-0.5 text-foreground">{plan.maxTeamMembers}</dd>
                    </div>
                    <div>
                        <dt className="text-muted-foreground">Max Departments</dt>
                        <dd className="mt-0.5 text-foreground">{plan.maxDepartments}</dd>
                    </div>
                    <div>
                        <dt className="text-muted-foreground">Max Stories/Month</dt>
                        <dd className="mt-0.5 text-foreground">{plan.maxStoriesPerMonth}</dd>
                    </div>
                </dl>
            </div>

            {/* Usage Meters */}
            <div className="rounded-lg border border-border bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-card-foreground">Usage</h2>
                {usage.metrics.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No usage data available.</p>
                ) : (
                    <div className="space-y-4">
                        {usage.metrics.map((m) => (
                            <div key={m.metricName}>
                                <div className="mb-1 flex items-center justify-between text-sm">
                                    <span className="font-medium text-foreground">{formatMetricName(m.metricName)}</span>
                                    <span className="text-muted-foreground">
                                        {m.currentValue.toLocaleString()} / {m.limit.toLocaleString()} ({m.percentUsed.toFixed(1)}%)
                                    </span>
                                </div>
                                <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                                    <div
                                        className={`h-full rounded-full transition-all ${m.percentUsed >= 90 ? 'bg-destructive' : m.percentUsed >= 70 ? 'bg-yellow-500' : 'bg-primary'}`}
                                        style={{ width: `${Math.min(m.percentUsed, 100)}%` }}
                                    />
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            <OverridePlanModal
                open={overrideOpen}
                onClose={() => setOverrideOpen(false)}
                onSubmit={handleOverride}
                plans={plans}
                currentPlanId={subscription.planId}
            />

            <CancelSubscriptionModal
                open={cancelOpen}
                onClose={() => setCancelOpen(false)}
                onSubmit={handleCancel}
            />
        </div>
    );
}

function formatMetricName(name: string): string {
    return name
        .replace(/_/g, ' ')
        .replace(/\b\w/g, (c) => c.toUpperCase());
}
