import { createApiClient } from './client';
import { env } from '@/utils/env';
import type { PaginatedResponse } from '@/types/api';
import type {
    RiskRegisterResponse,
    CreateRiskRequest,
    UpdateRiskRequest,
} from '@/types/analytics';

const client = createApiClient({ baseURL: env.WORK_API_URL });

export const riskRegisterApi = {
    getRisks: (params: {
        projectId: string;
        sprintId?: string;
        severity?: string;
        mitigationStatus?: string;
        page?: number;
        pageSize?: number;
    }): Promise<PaginatedResponse<RiskRegisterResponse>> =>
        client.get('/api/v1/analytics/risks', { params }).then((r) => r.data),

    createRisk: (data: CreateRiskRequest): Promise<RiskRegisterResponse> =>
        client.post('/api/v1/analytics/risks', data).then((r) => r.data),

    updateRisk: (
        riskId: string,
        data: UpdateRiskRequest
    ): Promise<RiskRegisterResponse> =>
        client.put(`/api/v1/analytics/risks/${riskId}`, data).then((r) => r.data),

    deleteRisk: (riskId: string): Promise<void> =>
        client.delete(`/api/v1/analytics/risks/${riskId}`).then(() => undefined),
};
