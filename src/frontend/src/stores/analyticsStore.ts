import { create } from 'zustand';
import type {
    VelocitySnapshotResponse,
    ResourceManagementResponse,
    ResourceUtilizationDetailResponse,
    ProjectCostAnalyticsResponse,
    ProjectHealthResponse,
    BugMetricsResponse,
    DashboardSummaryResponse,
    SnapshotStatusResponse,
} from '@/types/analytics';

interface AnalyticsState {
    velocityTrends: VelocitySnapshotResponse[];
    resourceManagement: ResourceManagementResponse[];
    resourceUtilization: ResourceUtilizationDetailResponse[];
    projectCost: ProjectCostAnalyticsResponse | null;
    projectHealth: ProjectHealthResponse | null;
    bugMetrics: BugMetricsResponse | null;
    dashboard: DashboardSummaryResponse | null;
    snapshotStatus: SnapshotStatusResponse | null;
    loading: boolean;
    error: string | null;
}

interface AnalyticsActions {
    fetchVelocityTrends(projectId: string, sprintCount?: number): Promise<void>;
    fetchResourceManagement(dateFrom?: string, dateTo?: string, departmentId?: string): Promise<void>;
    fetchResourceUtilization(projectId: string, dateFrom?: string, dateTo?: string): Promise<void>;
    fetchProjectCost(projectId: string, dateFrom?: string, dateTo?: string): Promise<void>;
    fetchProjectHealth(projectId: string, history?: boolean): Promise<void>;
    fetchBugMetrics(projectId: string, sprintId?: string): Promise<void>;
    fetchDashboard(projectId: string): Promise<void>;
    fetchSnapshotStatus(): Promise<void>;
    reset(): void;
}

const initialState: AnalyticsState = {
    velocityTrends: [],
    resourceManagement: [],
    resourceUtilization: [],
    projectCost: null,
    projectHealth: null,
    bugMetrics: null,
    dashboard: null,
    snapshotStatus: null,
    loading: false,
    error: null,
};

export const useAnalyticsStore = create<AnalyticsState & AnalyticsActions>()(
    (set) => ({
        ...initialState,

        async fetchVelocityTrends(projectId: string, sprintCount?: number) {
            set({ loading: true, error: null });
            try {
                const { analyticsApi } = await import('@/api/analyticsApi');
                const velocityTrends = await analyticsApi.getVelocityTrends({
                    projectId,
                    sprintCount,
                });
                set({ velocityTrends, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch velocity trends',
                });
            }
        },

        async fetchResourceManagement(dateFrom?: string, dateTo?: string, departmentId?: string) {
            set({ loading: true, error: null });
            try {
                const { analyticsApi } = await import('@/api/analyticsApi');
                const resourceManagement = await analyticsApi.getResourceManagement({
                    dateFrom,
                    dateTo,
                    departmentId,
                });
                set({ resourceManagement, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch resource management',
                });
            }
        },

        async fetchResourceUtilization(projectId: string, dateFrom?: string, dateTo?: string) {
            set({ loading: true, error: null });
            try {
                const { analyticsApi } = await import('@/api/analyticsApi');
                const resourceUtilization = await analyticsApi.getResourceUtilization({
                    projectId,
                    dateFrom,
                    dateTo,
                });
                set({ resourceUtilization, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch resource utilization',
                });
            }
        },

        async fetchProjectCost(projectId: string, dateFrom?: string, dateTo?: string) {
            set({ loading: true, error: null });
            try {
                const { analyticsApi } = await import('@/api/analyticsApi');
                const projectCost = await analyticsApi.getProjectCost({
                    projectId,
                    dateFrom,
                    dateTo,
                });
                set({ projectCost, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch project cost',
                });
            }
        },

        async fetchProjectHealth(projectId: string, history?: boolean) {
            set({ loading: true, error: null });
            try {
                const { analyticsApi } = await import('@/api/analyticsApi');
                const projectHealth = await analyticsApi.getProjectHealth({
                    projectId,
                    history,
                });
                set({ projectHealth, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch project health',
                });
            }
        },

        async fetchBugMetrics(projectId: string, sprintId?: string) {
            set({ loading: true, error: null });
            try {
                const { analyticsApi } = await import('@/api/analyticsApi');
                const bugMetrics = await analyticsApi.getBugMetrics({
                    projectId,
                    sprintId,
                });
                set({ bugMetrics, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch bug metrics',
                });
            }
        },

        async fetchDashboard(projectId: string) {
            set({ loading: true, error: null });
            try {
                const { analyticsApi } = await import('@/api/analyticsApi');
                const dashboard = await analyticsApi.getDashboard({ projectId });
                set({ dashboard, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch dashboard',
                });
            }
        },

        async fetchSnapshotStatus() {
            set({ loading: true, error: null });
            try {
                const { analyticsApi } = await import('@/api/analyticsApi');
                const snapshotStatus = await analyticsApi.getSnapshotStatus();
                set({ snapshotStatus, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch snapshot status',
                });
            }
        },

        reset() {
            set(initialState);
        },
    })
);
