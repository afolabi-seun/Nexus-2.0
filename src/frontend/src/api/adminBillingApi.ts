import { createApiClient } from './client';
import { env } from '@/utils/env';
import type {
    AdminSubscriptionListItem,
    PaginatedResponse,
    AdminOrganizationBillingResponse,
    AdminOverrideRequest,
    AdminCancelRequest,
    AdminUsageSummary,
    AdminOrganizationUsageItem,
    AdminPlanResponse,
    AdminCreatePlanRequest,
    AdminUpdatePlanRequest,
} from '@/types/adminBilling';

const client = createApiClient({ baseURL: env.BILLING_API_URL });

export const adminBillingApi = {
    getSubscriptions: (params?: {
        status?: string;
        search?: string;
        page?: number;
        pageSize?: number;
    }): Promise<PaginatedResponse<AdminSubscriptionListItem>> =>
        client
            .get('/api/v1/admin/billing/subscriptions', { params })
            .then((r) => r.data),

    getOrganizationBilling: (
        organizationId: string
    ): Promise<AdminOrganizationBillingResponse> =>
        client
            .get(`/api/v1/admin/billing/organizations/${organizationId}`)
            .then((r) => r.data),

    overrideSubscription: (
        organizationId: string,
        data: AdminOverrideRequest
    ): Promise<void> =>
        client
            .post(`/api/v1/admin/billing/organizations/${organizationId}/override`, data)
            .then((r) => r.data),

    cancelSubscription: (
        organizationId: string,
        data: AdminCancelRequest
    ): Promise<void> =>
        client
            .post(`/api/v1/admin/billing/organizations/${organizationId}/cancel`, data)
            .then((r) => r.data),

    getUsageSummary: (): Promise<AdminUsageSummary> =>
        client.get('/api/v1/admin/billing/usage/summary').then((r) => r.data),

    getOrganizationUsage: (params?: {
        threshold?: number;
        page?: number;
        pageSize?: number;
    }): Promise<PaginatedResponse<AdminOrganizationUsageItem>> =>
        client
            .get('/api/v1/admin/billing/usage/organizations', { params })
            .then((r) => r.data),

    getPlans: (): Promise<AdminPlanResponse[]> =>
        client.get('/api/v1/admin/billing/plans').then((r) => r.data),

    createPlan: (data: AdminCreatePlanRequest): Promise<AdminPlanResponse> =>
        client.post('/api/v1/admin/billing/plans', data).then((r) => r.data),

    updatePlan: (
        planId: string,
        data: AdminUpdatePlanRequest
    ): Promise<AdminPlanResponse> =>
        client
            .put(`/api/v1/admin/billing/plans/${planId}`, data)
            .then((r) => r.data),

    deactivatePlan: (planId: string): Promise<AdminPlanResponse> =>
        client
            .patch(`/api/v1/admin/billing/plans/${planId}/deactivate`)
            .then((r) => r.data),
};
