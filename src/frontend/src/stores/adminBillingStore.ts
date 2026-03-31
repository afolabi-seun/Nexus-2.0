import { create } from 'zustand';
import type {
    AdminSubscriptionListItem,
    AdminPlanResponse,
    AdminUsageSummary,
    AdminOrganizationBillingResponse,
    AdminOrganizationUsageItem,
} from '@/types/adminBilling';

interface AdminBillingFilters {
    status?: string;
    search?: string;
    threshold?: number;
}

interface AdminBillingState {
    subscriptions: AdminSubscriptionListItem[];
    subscriptionsTotalCount: number;
    subscriptionsPage: number;
    subscriptionsPageSize: number;

    plans: AdminPlanResponse[];

    usageSummary: AdminUsageSummary | null;

    organizationBilling: AdminOrganizationBillingResponse | null;

    organizationUsage: AdminOrganizationUsageItem[];
    organizationUsageTotalCount: number;
    organizationUsagePage: number;
    organizationUsagePageSize: number;

    filters: AdminBillingFilters;

    loading: boolean;
    error: string | null;
}

interface AdminBillingActions {
    setFilters(filters: Partial<AdminBillingFilters>): void;
    fetchSubscriptions(page?: number, pageSize?: number): Promise<void>;
    fetchPlans(): Promise<void>;
    fetchUsageSummary(): Promise<void>;
    fetchOrganizationBilling(organizationId: string): Promise<void>;
    fetchOrganizationUsage(page?: number, pageSize?: number): Promise<void>;
    reset(): void;
}

const initialState: AdminBillingState = {
    subscriptions: [],
    subscriptionsTotalCount: 0,
    subscriptionsPage: 1,
    subscriptionsPageSize: 20,
    plans: [],
    usageSummary: null,
    organizationBilling: null,
    organizationUsage: [],
    organizationUsageTotalCount: 0,
    organizationUsagePage: 1,
    organizationUsagePageSize: 20,
    filters: {},
    loading: false,
    error: null,
};

export const useAdminBillingStore = create<AdminBillingState & AdminBillingActions>()(
    (set, get) => ({
        ...initialState,

        setFilters(filters) {
            set({ filters: { ...get().filters, ...filters } });
        },

        async fetchSubscriptions(page?: number, pageSize?: number) {
            set({ loading: true, error: null });
            try {
                const { adminBillingApi } = await import('@/api/adminBillingApi');
                const { filters } = get();
                const p = page ?? get().subscriptionsPage;
                const ps = pageSize ?? get().subscriptionsPageSize;
                const result = await adminBillingApi.getSubscriptions({
                    status: filters.status,
                    search: filters.search,
                    page: p,
                    pageSize: ps,
                });
                set({
                    subscriptions: result.items,
                    subscriptionsTotalCount: result.totalCount,
                    subscriptionsPage: result.page,
                    subscriptionsPageSize: result.pageSize,
                    loading: false,
                });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch subscriptions',
                });
            }
        },

        async fetchPlans() {
            set({ loading: true, error: null });
            try {
                const { adminBillingApi } = await import('@/api/adminBillingApi');
                const plans = await adminBillingApi.getPlans();
                set({ plans, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch plans',
                });
            }
        },

        async fetchUsageSummary() {
            set({ loading: true, error: null });
            try {
                const { adminBillingApi } = await import('@/api/adminBillingApi');
                const usageSummary = await adminBillingApi.getUsageSummary();
                set({ usageSummary, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch usage summary',
                });
            }
        },

        async fetchOrganizationBilling(organizationId: string) {
            set({ loading: true, error: null });
            try {
                const { adminBillingApi } = await import('@/api/adminBillingApi');
                const organizationBilling =
                    await adminBillingApi.getOrganizationBilling(organizationId);
                set({ organizationBilling, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error:
                        err instanceof Error
                            ? err.message
                            : 'Failed to fetch organization billing',
                });
            }
        },

        async fetchOrganizationUsage(page?: number, pageSize?: number) {
            set({ loading: true, error: null });
            try {
                const { adminBillingApi } = await import('@/api/adminBillingApi');
                const { filters } = get();
                const p = page ?? get().organizationUsagePage;
                const ps = pageSize ?? get().organizationUsagePageSize;
                const result = await adminBillingApi.getOrganizationUsage({
                    threshold: filters.threshold,
                    page: p,
                    pageSize: ps,
                });
                set({
                    organizationUsage: result.items,
                    organizationUsageTotalCount: result.totalCount,
                    organizationUsagePage: result.page,
                    organizationUsagePageSize: result.pageSize,
                    loading: false,
                });
            } catch (err) {
                set({
                    loading: false,
                    error:
                        err instanceof Error
                            ? err.message
                            : 'Failed to fetch organization usage',
                });
            }
        },

        reset() {
            set(initialState);
        },
    })
);
