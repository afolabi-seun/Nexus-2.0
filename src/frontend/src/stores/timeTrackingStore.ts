import { create } from 'zustand';
import type {
    TimeEntryResponse,
    TimerStatusResponse,
    CostRateResponse,
    TimePolicyResponse,
    CreateTimeEntryRequest,
    UpdateTimeEntryRequest,
    RejectTimeEntryRequest,
    TimerStartRequest,
    CreateCostRateRequest,
    UpdateCostRateRequest,
    UpdateTimePolicyRequest,
} from '@/types/timeTracking';

interface TimeTrackingState {
    timeEntries: TimeEntryResponse[];
    totalCount: number;
    timerStatus: TimerStatusResponse | null;
    costRates: CostRateResponse[];
    costRateTotalCount: number;
    timePolicy: TimePolicyResponse | null;
    loading: boolean;
    error: string | null;
}

interface TimeTrackingActions {
    fetchTimeEntries(params?: {
        storyId?: string; projectId?: string; sprintId?: string;
        memberId?: string; dateFrom?: string; dateTo?: string;
        billable?: boolean; status?: string; page?: number; pageSize?: number;
    }): Promise<void>;
    createTimeEntry(data: CreateTimeEntryRequest): Promise<void>;
    updateTimeEntry(id: string, data: UpdateTimeEntryRequest): Promise<void>;
    deleteTimeEntry(id: string): Promise<void>;
    approveTimeEntry(id: string): Promise<void>;
    rejectTimeEntry(id: string, data: RejectTimeEntryRequest): Promise<void>;
    startTimer(data: TimerStartRequest): Promise<void>;
    stopTimer(): Promise<void>;
    fetchTimerStatus(): Promise<void>;
    fetchCostRates(params?: { rateType?: string; memberId?: string; page?: number; pageSize?: number }): Promise<void>;
    createCostRate(data: CreateCostRateRequest): Promise<void>;
    updateCostRate(id: string, data: UpdateCostRateRequest): Promise<void>;
    deleteCostRate(id: string): Promise<void>;
    fetchTimePolicy(): Promise<void>;
    updateTimePolicy(data: UpdateTimePolicyRequest): Promise<void>;
    reset(): void;
}

const initialState: TimeTrackingState = {
    timeEntries: [],
    totalCount: 0,
    timerStatus: null,
    costRates: [],
    costRateTotalCount: 0,
    timePolicy: null,
    loading: false,
    error: null,
};

export const useTimeTrackingStore = create<TimeTrackingState & TimeTrackingActions>()(
    (set) => ({
        ...initialState,

        async fetchTimeEntries(params) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                const result = await timeTrackingApi.listTimeEntries(params);
                set({ timeEntries: result.data, totalCount: result.totalCount, loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to fetch time entries' });
            }
        },

        async createTimeEntry(data) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                await timeTrackingApi.createTimeEntry(data);
                set({ loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to create time entry' });
            }
        },

        async updateTimeEntry(id, data) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                await timeTrackingApi.updateTimeEntry(id, data);
                set({ loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to update time entry' });
            }
        },

        async deleteTimeEntry(id) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                await timeTrackingApi.deleteTimeEntry(id);
                set({ loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to delete time entry' });
            }
        },

        async approveTimeEntry(id) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                await timeTrackingApi.approveTimeEntry(id);
                set({ loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to approve time entry' });
            }
        },

        async rejectTimeEntry(id, data) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                await timeTrackingApi.rejectTimeEntry(id, data);
                set({ loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to reject time entry' });
            }
        },

        async startTimer(data) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                const timerStatus = await timeTrackingApi.startTimer(data);
                set({ timerStatus, loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to start timer' });
            }
        },

        async stopTimer() {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                await timeTrackingApi.stopTimer();
                set({ timerStatus: null, loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to stop timer' });
            }
        },

        async fetchTimerStatus() {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                const timerStatus = await timeTrackingApi.getTimerStatus();
                set({ timerStatus, loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to fetch timer status' });
            }
        },

        async fetchCostRates(params) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                const result = await timeTrackingApi.listCostRates(params);
                set({ costRates: result.data, costRateTotalCount: result.totalCount, loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to fetch cost rates' });
            }
        },

        async createCostRate(data) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                await timeTrackingApi.createCostRate(data);
                set({ loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to create cost rate' });
            }
        },

        async updateCostRate(id, data) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                await timeTrackingApi.updateCostRate(id, data);
                set({ loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to update cost rate' });
            }
        },

        async deleteCostRate(id) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                await timeTrackingApi.deleteCostRate(id);
                set({ loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to delete cost rate' });
            }
        },

        async fetchTimePolicy() {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                const timePolicy = await timeTrackingApi.getTimePolicy();
                set({ timePolicy, loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to fetch time policy' });
            }
        },

        async updateTimePolicy(data) {
            set({ loading: true, error: null });
            try {
                const { timeTrackingApi } = await import('@/api/timeTrackingApi');
                const timePolicy = await timeTrackingApi.updateTimePolicy(data);
                set({ timePolicy, loading: false });
            } catch (err) {
                set({ loading: false, error: err instanceof Error ? err.message : 'Failed to update time policy' });
            }
        },

        reset() {
            set(initialState);
        },
    })
);
