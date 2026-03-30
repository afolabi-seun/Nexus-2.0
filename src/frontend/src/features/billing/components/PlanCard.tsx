import { Check, X } from 'lucide-react';
import type { PlanResponse, PlanFeatures } from '@/types/billing';

interface PlanCardProps {
    plan: PlanResponse;
    currentPlanTierLevel: number | null;
    subscriptionStatus: string | null;
    isCurrentPlan: boolean;
    onUpgrade: (planId: string, planName: string) => void;
    onDowngrade: (planId: string, planName: string) => void;
    onSelect: (planId: string, planName: string) => void;
    loading: boolean;
}

function parsePlanFeatures(featuresJson: string | null): PlanFeatures {
    if (!featuresJson) {
        return { sprintAnalytics: 'none', customWorkflows: false, prioritySupport: false };
    }
    try {
        return JSON.parse(featuresJson) as PlanFeatures;
    } catch {
        return { sprintAnalytics: 'none', customWorkflows: false, prioritySupport: false };
    }
}

function formatLimit(value: number): string {
    return value === 0 ? 'Unlimited' : String(value);
}

export function PlanCard({
    plan,
    currentPlanTierLevel,
    subscriptionStatus,
    isCurrentPlan,
    onUpgrade,
    onDowngrade,
    onSelect,
    loading,
}: PlanCardProps) {
    const features = parsePlanFeatures(plan.featuresJson);
    const priceMonthly = plan.priceMonthly === 0 ? 'Free' : `$${plan.priceMonthly.toFixed(2)}/mo`;
    const priceYearly = plan.priceYearly === 0 ? '' : `$${plan.priceYearly.toFixed(2)}/yr`;

    const hasActiveSub = currentPlanTierLevel !== null &&
        (subscriptionStatus === 'Active' || subscriptionStatus === 'Trialing');

    let buttonLabel: string;
    let buttonAction: (() => void) | null = null;
    let buttonDisabled = false;
    let buttonStyle = 'bg-primary hover:bg-primary/90 text-white';

    if (isCurrentPlan) {
        buttonLabel = 'Current Plan';
        buttonDisabled = true;
        buttonStyle = 'bg-muted text-muted-foreground cursor-not-allowed';
    } else if (hasActiveSub) {
        if (plan.tierLevel > currentPlanTierLevel!) {
            buttonLabel = 'Upgrade';
            buttonAction = () => onUpgrade(plan.planId, plan.planName);
        } else {
            buttonLabel = 'Downgrade';
            buttonAction = () => onDowngrade(plan.planId, plan.planName);
            buttonStyle = 'bg-destructive hover:bg-destructive/90 text-white';
        }
    } else {
        buttonLabel = 'Select Plan';
        buttonAction = () => onSelect(plan.planId, plan.planName);
    }

    const featureRows = [
        { label: 'Max Team Members', value: formatLimit(plan.maxTeamMembers) },
        { label: 'Max Departments', value: formatLimit(plan.maxDepartments) },
        { label: 'Max Stories/Month', value: formatLimit(plan.maxStoriesPerMonth) },
        { label: 'Sprint Analytics', value: features.sprintAnalytics.charAt(0).toUpperCase() + features.sprintAnalytics.slice(1), type: 'string' as const },
        { label: 'Custom Workflows', value: features.customWorkflows, type: 'boolean' as const },
        { label: 'Priority Support', value: features.prioritySupport, type: 'boolean' as const },
    ];

    return (
        <div className={`flex flex-col rounded-lg border p-6 ${isCurrentPlan ? 'border-primary border-2 bg-primary/5' : 'border-border bg-card'}`}>
            <div className="mb-4">
                <div className="flex items-center gap-2">
                    <h3 className="text-lg font-semibold text-card-foreground">{plan.planName}</h3>
                    {isCurrentPlan && (
                        <span className="rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
                            Current Plan
                        </span>
                    )}
                </div>
                <div className="mt-1">
                    <span className="text-2xl font-bold text-foreground">{priceMonthly}</span>
                    {priceYearly && (
                        <span className="ml-2 text-sm text-muted-foreground">{priceYearly}</span>
                    )}
                </div>
            </div>

            <div className="flex-1 space-y-3 text-sm">
                {featureRows.map((row) => (
                    <div key={row.label} className="flex items-center justify-between">
                        <span className="text-muted-foreground">{row.label}</span>
                        <span className="font-medium text-foreground">
                            {row.type === 'boolean' ? (
                                row.value ? <Check size={16} className="text-green-500" /> : <X size={16} className="text-muted-foreground" />
                            ) : (
                                String(row.value)
                            )}
                        </span>
                    </div>
                ))}
            </div>

            <button
                onClick={buttonAction ?? undefined}
                disabled={buttonDisabled || loading}
                className={`mt-6 w-full rounded-md px-4 py-2 text-sm font-medium disabled:opacity-50 ${buttonStyle}`}
            >
                {loading && !buttonDisabled ? 'Processing…' : buttonLabel}
            </button>
        </div>
    );
}
