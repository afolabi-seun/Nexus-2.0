import { useState } from 'react';
import { Link } from 'react-router-dom';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { useToast } from '@/components/common/Toast';
import { billingApi } from '@/api/billingApi';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { SubscriptionResponse } from '@/types/billing';

interface SubscriptionActionsProps {
    subscription: SubscriptionResponse;
    onCancelSuccess: () => void;
}

export function SubscriptionActions({ subscription, onCancelSuccess }: SubscriptionActionsProps) {
    const { addToast } = useToast();
    const [showConfirm, setShowConfirm] = useState(false);
    const [loading, setLoading] = useState(false);

    const isActiveOrTrialing = subscription.status === 'Active' || subscription.status === 'Trialing';
    const isCancelledOrExpired = subscription.status === 'Cancelled' || subscription.status === 'Expired';
    const isFree = subscription.planCode === 'free';
    const showCancel = isActiveOrTrialing && !isFree;

    async function handleCancel() {
        setLoading(true);
        try {
            await billingApi.cancelSubscription();
            addToast('success', 'Subscription cancelled');
            setShowConfirm(false);
            onCancelSuccess();
        } catch (err) {
            const message = err instanceof ApiError ? mapErrorCode(err.errorCode) : 'Something went wrong. Please try again.';
            addToast('error', message);
            setShowConfirm(false);
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="flex flex-wrap gap-3">
            {isActiveOrTrialing && (
                <>
                    <Link
                        to="/billing/plans"
                        className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
                    >
                        Upgrade Plan
                    </Link>
                    <Link
                        to="/billing/plans"
                        className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent"
                    >
                        Change Plan
                    </Link>
                </>
            )}

            {showCancel && (
                <button
                    onClick={() => setShowConfirm(true)}
                    disabled={loading}
                    className="rounded-md bg-destructive px-4 py-2 text-sm font-medium text-white hover:bg-destructive/90 disabled:opacity-50"
                >
                    {loading ? 'Cancelling…' : 'Cancel Subscription'}
                </button>
            )}

            {isCancelledOrExpired && (
                <Link
                    to="/billing/plans"
                    className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
                >
                    Resubscribe
                </Link>
            )}

            <ConfirmDialog
                open={showConfirm}
                onConfirm={handleCancel}
                onCancel={() => setShowConfirm(false)}
                title="Cancel Subscription"
                message="Are you sure you want to cancel? Your access will continue until the end of the current billing period."
                confirmLabel="Cancel Subscription"
                destructive
            />
        </div>
    );
}
