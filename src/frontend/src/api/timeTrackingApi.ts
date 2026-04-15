import { createApiClient } from './client';
import { env } from '@/utils/env';
import type {
    CreateTimeEntryRequest,
    UpdateTimeEntryRequest,
    TimeEntryResponse,
    RejectTimeEntryRequest,
    TimerStartRequest,
    TimerStatusResponse,
    CreateCostRateRequest,
    UpdateCostRateRequest,
    CostRateResponse,
    UpdateTimePolicyRequest,
    TimePolicyResponse,
} from '@/types/timeTracking';

const client = createApiClient({ baseURL: env.WORK_API_URL });

export const timeTrackingApi = {
    // ── Time Entries ──

    createTimeEntry: (data: CreateTimeEntryRequest): Promise<TimeEntryResponse> =>
        client.post('/api/v1/time-entries', data).then((r) => r.data),

    updateTimeEntry: (id: string, data: UpdateTimeEntryRequest): Promise<TimeEntryResponse> =>
        client.put(`/api/v1/time-entries/${id}`, data).then((r) => r.data),

    deleteTimeEntry: (id: string): Promise<void> =>
        client.delete(`/api/v1/time-entries/${id}`).then(() => undefined),

    listTimeEntries: (params?: {
        storyId?: string;
        projectId?: string;
        sprintId?: string;
        memberId?: string;
        dateFrom?: string;
        dateTo?: string;
        billable?: boolean;
        status?: string;
        page?: number;
        pageSize?: number;
    }): Promise<{ data: TimeEntryResponse[]; totalCount: number }> =>
        client.get('/api/v1/time-entries', { params }).then((r) => r.data),

    approveTimeEntry: (id: string): Promise<TimeEntryResponse> =>
        client.post(`/api/v1/time-entries/${id}/approve`).then((r) => r.data),

    rejectTimeEntry: (id: string, data: RejectTimeEntryRequest): Promise<TimeEntryResponse> =>
        client.post(`/api/v1/time-entries/${id}/reject`, data).then((r) => r.data),

    // ── Timer ──

    startTimer: (data: TimerStartRequest): Promise<TimerStatusResponse> =>
        client.post('/api/v1/timer/start', data).then((r) => r.data),

    stopTimer: (): Promise<TimeEntryResponse> =>
        client.post('/api/v1/timer/stop').then((r) => r.data),

    getTimerStatus: (): Promise<TimerStatusResponse> =>
        client.get('/api/v1/timer/status').then((r) => r.data),

    // ── Cost Rates ──

    createCostRate: (data: CreateCostRateRequest): Promise<CostRateResponse> =>
        client.post('/api/v1/cost-rates', data).then((r) => r.data),

    updateCostRate: (id: string, data: UpdateCostRateRequest): Promise<CostRateResponse> =>
        client.put(`/api/v1/cost-rates/${id}`, data).then((r) => r.data),

    deleteCostRate: (id: string): Promise<void> =>
        client.delete(`/api/v1/cost-rates/${id}`).then(() => undefined),

    listCostRates: (params?: {
        rateType?: string;
        memberId?: string;
        page?: number;
        pageSize?: number;
    }): Promise<{ data: CostRateResponse[]; totalCount: number }> =>
        client.get('/api/v1/cost-rates', { params }).then((r) => r.data),

    // ── Time Policy ──

    getTimePolicy: (): Promise<TimePolicyResponse> =>
        client.get('/api/v1/time-policies').then((r) => r.data),

    updateTimePolicy: (data: UpdateTimePolicyRequest): Promise<TimePolicyResponse> =>
        client.put('/api/v1/time-policies', data).then((r) => r.data),
};
