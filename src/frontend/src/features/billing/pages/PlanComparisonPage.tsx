import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { billingApi } from '@/api/billingApi';
import { ApiError } from '@/types/api';
import { mapErrorCode } from '@/utils/errorMapping';
import { useToast } from '@/components/common/Toast';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { PlanCard } from '../components/PlanCard';
import type { PlanResponse, SubscriptionResponse } from '@/types/billing';

export function PlanComparisonPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();

    const [plans, setPlans] = useState<PlanResponse[]>([]);
    const [subscription, setSubscription] = useState<SubscriptionResponse | null>(null);
    const [loading, setLoading] = useState(true);
    const [actionLoading, setActionLoading] = useState(false);

    const [confirm, setConfirm] = useState<{
        open: boolean;
        title: string;
        message: string;
        confirmLabel: string;
        destructive: boolean;
        action: () => Promise<void>;
    }>({ open: false, title: '', message: '', confirmLabel: '', destructive: false, action: async () => { } });

    const fetchData = useCallback(async () => {
        setLoading(true);
        try {
            const [plansData, subData] = await Promise.allSettled([
                billingApi.getPlans(),
                billingApi.getCurrentSubscription(),
            ]);

            if (plansData.status === 'fulfilled') {
                setPlans([...plansData.value].sort((a, b) => a.tierLevel - b.tierLevel));
            }

            if (subData.status === 'fulfilled') {
                setSubscription(subData.value.subscription);
            } else {
                setSubscription(null);
            }
        } catch {
            addToast('error', 'Failed to load plans.');
        } finally {
            setLoading(false);
        }
    }, [addToast]);

    useEffect(() => { fetchData(); }, [fetchData]);

    async function handleAction(apiCall: () => Promise<unknown>, successMsg: string) {
        setActionLoading(true);
        try {
            await apiCall();
            addToast('success', successMsg);
            navigate('/billing');
        } catch (err) {
            const message = err instanceof ApiError ? mapErrorCode(err.errorCode) : 'Something went wrong. Please try again.';
            addToast('error', message);
        } finally {
            setActionLoading(false);
            setConfirm((c) => ({ ...c, open: false }));
        }
    }

    function onUpgrade(planId: string, planName: string) {
        setConfirm({
            open: true,
            title: `Upgrade to ${planName}`,
            message: 'You will be upgraded immediately. Prorated charges will apply for the remainder of the current billing period.',
            confirmLabel: 'Confirm Upgrade',
            destructive: false,
            action: () => handleAction(
                () => billingApi.upgradeSubscription({ newPlanId: planId }),
                `Plan upgraded to ${planName}`,
            ),
        });
    }

    function onDowngrade(planId: string, planName: string) {
        setConfirm({
            open: true,
            title: `Downgrade to ${planName}`,
            message: 'The downgrade will take effect at the end of your current billing period. Your current plan features will remain available until then.',
            confirmLabel: 'Confirm Downgrade',
            destructive: true,
            action: () => handleAction(
                () => billingApi.downgradeSubscription({ newPlanId: planId }),
                `Downgrade to ${planName} scheduled`,
            ),
        });
    }

    function onSelect(planId: string, planName: string) {
        handleAction(
            () => billingApi.createSubscription({ planId, paymentMethodToken: null }),
            `Subscribed to ${planName}`,
        );
    }

    const currentTier = subscription ? subscription.planId : null;
    const currentTierLevel = subscription
        ? plans.find((p) => p.planId === subscription.planId)?.tierLevel ?? null
        : null;

    if (loading) {
        return (
            <div className="space-y-6 p-6">
                <h1 className="text-2xl font-bold text-foreground">Choose a Plan</h1>
                <div className="grid gap-6 grid-cols-1 md:grid-cols-2 lg:grid-cols-4">
                    {Array.from({ length: 4 }).map((_, i) => (
                        <SkeletonLoader key={i} variant="card" />
                    ))}
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-6 p-6">
            <h1 className="text-2xl font-bold text-foreground">Choose a Plan</h1>

            <div className="grid gap-6 grid-cols-1 md:grid-cols-2 lg:grid-cols-4">
                {plans.map((plan) => (
                    <PlanCard
                        key={plan.planId}
                        plan={plan}
                        currentPlanTierLevel={currentTierLevel}
                        subscriptionStatus={subscription?.status ?? null}
                        isCurrentPlan={plan.planId === currentTier}
                        onUpgrade={onUpgrade}
                        onDowngrade={onDowngrade}
                        onSelect={onSelect}
                        loading={actionLoading}
                    />
                ))}
            </div>

            <ConfirmDialog
                open={confirm.open}
                onConfirm={() => confirm.action()}
                onCancel={() => setConfirm((c) => ({ ...c, open: false }))}
                title={confirm.title}
                message={confirm.message}
                confirmLabel={confirm.confirmLabel}
                destructive={confirm.destructive}
            />
        </div>
    );
}
