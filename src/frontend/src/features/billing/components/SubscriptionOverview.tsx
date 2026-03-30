import { Badge } from '@/components/common/Badge';
import type { SubscriptionResponse } from '@/types/billing';

interface SubscriptionOverviewProps {
    subscription: SubscriptionResponse;
}

function formatDate(dateStr: string | null): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString();
}

export function SubscriptionOverview({ subscription }: SubscriptionOverviewProps) {
    return (
        <div className="rounded-lg border border-border bg-card p-6">
            <div className="mb-4 flex items-center gap-3">
                <h3 className="text-lg font-semibold text-card-foreground">
                    {subscription.planName}
                </h3>
                <Badge variant="status" value={subscription.status} />
            </div>

            <div className="space-y-2 text-sm text-muted-foreground">
                <div>
                    <span className="font-medium text-foreground">Billing Period: </span>
                    {formatDate(subscription.currentPeriodStart)} – {formatDate(subscription.currentPeriodEnd)}
                </div>

                {subscription.status === 'Trialing' && subscription.trialEndDate && (
                    <div>
                        <span className="font-medium text-foreground">Trial Ends: </span>
                        {formatDate(subscription.trialEndDate)}
                    </div>
                )}

                {subscription.scheduledPlanId && subscription.scheduledPlanName && (
                    <div className="rounded-md bg-amber-50 p-3 text-amber-800 dark:bg-amber-900/20 dark:text-amber-300">
                        Your plan will change to {subscription.scheduledPlanName} at the end of the current billing period.
                    </div>
                )}

                {subscription.status === 'Cancelled' && subscription.cancelledAt && (
                    <div className="rounded-md bg-red-50 p-3 text-red-800 dark:bg-red-900/20 dark:text-red-300">
                        Your subscription was cancelled on {formatDate(subscription.cancelledAt)}. Access continues until {formatDate(subscription.currentPeriodEnd)}.
                    </div>
                )}
            </div>
        </div>
    );
}
