export interface PlanResponse {
    planId: string;
    planName: string;
    planCode: string;
    tierLevel: number;
    maxTeamMembers: number;
    maxDepartments: number;
    maxStoriesPerMonth: number;
    featuresJson: string | null;
    priceMonthly: number;
    priceYearly: number;
}

export interface PlanFeatures {
    sprintAnalytics: string;
    customWorkflows: boolean;
    prioritySupport: boolean;
}

export interface SubscriptionResponse {
    subscriptionId: string;
    organizationId: string;
    planId: string;
    planName: string;
    planCode: string;
    status: SubscriptionStatus;
    currentPeriodStart: string;
    currentPeriodEnd: string | null;
    trialEndDate: string | null;
    cancelledAt: string | null;
    scheduledPlanId: string | null;
    scheduledPlanName: string | null;
}

export interface SubscriptionDetailResponse {
    subscription: SubscriptionResponse;
    plan: PlanResponse;
    usage: UsageResponse;
}

export interface UsageResponse {
    metrics: UsageMetric[];
}

export interface UsageMetric {
    metricName: string;
    currentValue: number;
    limit: number;
    percentUsed: number;
}

export interface CreateSubscriptionRequest {
    planId: string;
    paymentMethodToken: string | null;
}

export interface UpgradeSubscriptionRequest {
    newPlanId: string;
}

export interface DowngradeSubscriptionRequest {
    newPlanId: string;
}

export type SubscriptionStatus =
    | 'Active'
    | 'Trialing'
    | 'PastDue'
    | 'Cancelled'
    | 'Expired';
