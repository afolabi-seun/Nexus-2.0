import { createApiClient } from './client';
import { env } from '@/utils/env';
import type {
    VelocitySnapshotResponse,
    ResourceManagementResponse,
    ResourceUtilizationDetailResponse,
    ProjectCostAnalyticsResponse,
    ProjectHealthResponse,
    DependencyAnalysisResponse,
    BugMetricsResponse,
    DashboardSummaryResponse,
    SnapshotStatusResponse,
} from '@/types/analytics';

const client = createApiClient({ baseURL: env.WORK_API_URL });

export const analyticsApi = {
    getVelocityTrends: (params: {
        projectId: string;
        sprintCount?: number;
    }): Promise<VelocitySnapshotResponse[]> =>
        client.get('/api/v1/analytics/velocity', { params }).then((r) => r.data),

    getResourceManagement: (params?: {
        dateFrom?: string;
        dateTo?: string;
        departmentId?: string;
    }): Promise<ResourceManagementResponse[]> =>
        client.get('/api/v1/analytics/resource-management', { params }).then((r) => r.data),

    getResourceUtilization: (params: {
        projectId: string;
        dateFrom?: string;
        dateTo?: string;
    }): Promise<ResourceUtilizationDetailResponse[]> =>
        client.get('/api/v1/analytics/resource-utilization', { params }).then((r) => r.data),

    getProjectCost: (params: {
        projectId: string;
        dateFrom?: string;
        dateTo?: string;
    }): Promise<ProjectCostAnalyticsResponse> =>
        client.get('/api/v1/analytics/project-cost', { params }).then((r) => r.data),

    getProjectHealth: (params: {
        projectId: string;
        history?: boolean;
    }): Promise<ProjectHealthResponse> =>
        client.get('/api/v1/analytics/project-health', { params }).then((r) => r.data),

    getDependencies: (params: {
        projectId: string;
        sprintId?: string;
    }): Promise<DependencyAnalysisResponse> =>
        client.get('/api/v1/analytics/dependencies', { params }).then((r) => r.data),

    getBugMetrics: (params: {
        projectId: string;
        sprintId?: string;
    }): Promise<BugMetricsResponse> =>
        client.get('/api/v1/analytics/bugs', { params }).then((r) => r.data),

    getDashboard: (params: {
        projectId: string;
    }): Promise<DashboardSummaryResponse> =>
        client.get('/api/v1/analytics/dashboard', { params }).then((r) => r.data),

    getSnapshotStatus: (): Promise<SnapshotStatusResponse> =>
        client.get('/api/v1/analytics/snapshot-status').then((r) => r.data),
};
