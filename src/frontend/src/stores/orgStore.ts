import { create } from 'zustand';
import type { Organization, Department, NavigationItem } from '@/types/profile';
import type { ReferenceData } from '@/types/utility';

interface OrgState {
    organization: Organization | null;
    departments: Department[];
    referenceData: ReferenceData | null;
    navigation: NavigationItem[];
    navigationLoaded: boolean;
}

interface OrgActions {
    setOrganization(org: Organization): void;
    setDepartments(depts: Department[]): void;
    setReferenceData(data: ReferenceData): void;
    setNavigation(items: NavigationItem[]): void;
    refresh(): Promise<void>;
    refreshNavigation(): Promise<void>;
}

export const useOrgStore = create<OrgState & OrgActions>()((set) => ({
    organization: null,
    departments: [],
    referenceData: null,
    navigation: [],
    navigationLoaded: false,

    setOrganization(org) {
        set({ organization: org });
    },

    setDepartments(depts) {
        set({ departments: depts });
    },

    setReferenceData(data) {
        set({ referenceData: data });
    },

    setNavigation(items) {
        set({ navigation: items, navigationLoaded: true });
    },

    async refresh() {
        try {
            const { profileApi } = await import('@/api/profileApi');
            const { utilityApi } = await import('@/api/utilityApi');
            const { useAuthStore } = await import('@/stores/authStore');

            const user = useAuthStore.getState().user;
            if (!user?.organizationId) return;

            const [org, depts, refData, nav] = await Promise.all([
                profileApi.getOrganization(user.organizationId),
                profileApi.getDepartments().then((r) => r.data),
                utilityApi.getReferenceData(),
                profileApi.getNavigation().catch(() => []),
            ]);

            set({
                organization: org,
                departments: depts,
                referenceData: refData,
                navigation: nav,
                navigationLoaded: true,
            });
        } catch {
            // Silently fail — org data will be retried on next navigation
        }
    },

    async refreshNavigation() {
        try {
            const { profileApi } = await import('@/api/profileApi');
            const nav = await profileApi.getNavigation();
            set({ navigation: nav, navigationLoaded: true });
        } catch {
            // Fall through — sidebar will use fallback
        }
    },
}));
