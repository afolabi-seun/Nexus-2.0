import type { SubscriptionDetailResponse } from './billing';

export interface AdminSubscriptionListItem {
    subscriptionId: string;
    organizationId: string;
    organizationName: string;
    planId: string;
    planName: string;
    status: string;
    currentPeriodStart: string;
    currentPeriodEnd: string | null;
    trialEndDate: string | null;
}

export interface PaginatedResponse<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
}

export interface AdminUsageSummary {
    totalActiveMembers: number;
    totalStoriesCreated: number;
    totalStorageBytes: number;
    byPlanTier: PlanTierBreakdown[];
}

export interface PlanTierBreakdown {
    planName: string;
    planCode: string;
    organizationCount: number;
}

export interface AdminOrganizationUsageItem {
    organizationId: string;
    organizationName: string;
    planName: string;
    metrics: UsageMetricWithLimit[];
}

export interface UsageMetricWithLimit {
    metricName: string;
    currentValue: number;
    limit: number;
    percentUsed: number;
}

export interface AdminOverrideRequest {
    planId: string;
    reason?: string;
}

export interface AdminCancelRequest {
    reason?: string;
}

export interface AdminPlanResponse {
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
    isActive: boolean;
    dateCreated: string;
}

export interface AdminCreatePlanRequest {
    planName: string;
    planCode: string;
    tierLevel: number;
    maxTeamMembers: number;
    maxDepartments: number;
    maxStoriesPerMonth: number;
    priceMonthly: number;
    priceYearly: number;
    featuresJson?: string;
}

export interface AdminUpdatePlanRequest {
    planName: string;
    tierLevel: number;
    maxTeamMembers: number;
    maxDepartments: number;
    maxStoriesPerMonth: number;
    priceMonthly: number;
    priceYearly: number;
    featuresJson?: string;
}

/** Reuses the existing SubscriptionDetailResponse shape for single-org billing detail */
export type AdminOrganizationBillingResponse = SubscriptionDetailResponse;
