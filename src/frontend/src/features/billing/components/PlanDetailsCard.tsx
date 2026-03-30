import { Link } from 'react-router-dom';
import type { PlanResponse, PlanFeatures } from '@/types/billing';

interface PlanDetailsCardProps {
    plan: PlanResponse;
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

function formatBoolean(value: boolean): string {
    return value ? 'Included' : 'Not included';
}

export function PlanDetailsCard({ plan }: PlanDetailsCardProps) {
    const features = parsePlanFeatures(plan.featuresJson);
    const priceMonthly = plan.priceMonthly === 0 ? 'Free' : `$${plan.priceMonthly.toFixed(2)}/mo`;
    const priceYearly = plan.priceYearly === 0 ? 'Free' : `$${plan.priceYearly.toFixed(2)}/yr`;

    const rows = [
        { label: 'Max Team Members', value: formatLimit(plan.maxTeamMembers) },
        { label: 'Max Departments', value: formatLimit(plan.maxDepartments) },
        { label: 'Max Stories/Month', value: formatLimit(plan.maxStoriesPerMonth) },
        { label: 'Sprint Analytics', value: features.sprintAnalytics.charAt(0).toUpperCase() + features.sprintAnalytics.slice(1) },
        { label: 'Custom Workflows', value: formatBoolean(features.customWorkflows) },
        { label: 'Priority Support', value: formatBoolean(features.prioritySupport) },
    ];

    return (
        <div className="rounded-lg border border-border bg-card p-6">
            <div className="mb-4 flex items-center justify-between">
                <h3 className="text-lg font-semibold text-card-foreground">{plan.planName}</h3>
                <div className="text-right text-sm text-muted-foreground">
                    <div>{priceMonthly}</div>
                    <div>{priceYearly}</div>
                </div>
            </div>
            <table className="w-full text-sm">
                <tbody>
                    {rows.map((row) => (
                        <tr key={row.label} className="border-t border-border">
                            <td className="py-2 text-muted-foreground">{row.label}</td>
                            <td className="py-2 text-right font-medium text-foreground">{row.value}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
            <div className="mt-4">
                <Link
                    to="/billing/plans"
                    className="text-sm font-medium text-primary hover:underline"
                >
                    Compare Plans
                </Link>
            </div>
        </div>
    );
}
