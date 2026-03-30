import { createApiClient } from './client';
import { env } from '@/utils/env';
import type {
    PlanResponse,
    SubscriptionResponse,
    SubscriptionDetailResponse,
    UsageResponse,
    CreateSubscriptionRequest,
    UpgradeSubscriptionRequest,
    DowngradeSubscriptionRequest,
} from '@/types/billing';

const client = createApiClient({ baseURL: env.BILLING_API_URL });

export const billingApi = {
    getPlans: (): Promise<PlanResponse[]> =>
        client.get('/api/v1/plans').then((r) => r.data),

    getCurrentSubscription: (): Promise<SubscriptionDetailResponse> =>
        client.get('/api/v1/subscriptions/current').then((r) => r.data),

    createSubscription: (data: CreateSubscriptionRequest): Promise<SubscriptionResponse> =>
        client.post('/api/v1/subscriptions', data).then((r) => r.data),

    upgradeSubscription: (data: UpgradeSubscriptionRequest): Promise<SubscriptionResponse> =>
        client.patch('/api/v1/subscriptions/upgrade', data).then((r) => r.data),

    downgradeSubscription: (data: DowngradeSubscriptionRequest): Promise<SubscriptionResponse> =>
        client.patch('/api/v1/subscriptions/downgrade', data).then((r) => r.data),

    cancelSubscription: (): Promise<SubscriptionResponse> =>
        client.post('/api/v1/subscriptions/cancel').then((r) => r.data),

    getUsage: (): Promise<UsageResponse> =>
        client.get('/api/v1/usage').then((r) => r.data),
};
