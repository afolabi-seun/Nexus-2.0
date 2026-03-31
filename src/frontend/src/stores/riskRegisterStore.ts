import { create } from 'zustand';
import type {
    RiskRegisterResponse,
    CreateRiskRequest,
    UpdateRiskRequest,
} from '@/types/analytics';

interface RiskRegisterState {
    risks: RiskRegisterResponse[];
    totalCount: number;
    loading: boolean;
    error: string | null;
}

interface RiskRegisterActions {
    fetchRisks(
        projectId: string,
        sprintId?: string,
        severity?: string,
        mitigationStatus?: string,
        page?: number,
        pageSize?: number
    ): Promise<void>;
    createRisk(data: CreateRiskRequest): Promise<void>;
    updateRisk(riskId: string, data: UpdateRiskRequest): Promise<void>;
    deleteRisk(riskId: string): Promise<void>;
    reset(): void;
}

const initialState: RiskRegisterState = {
    risks: [],
    totalCount: 0,
    loading: false,
    error: null,
};

export const useRiskRegisterStore = create<RiskRegisterState & RiskRegisterActions>()(
    (set) => ({
        ...initialState,

        async fetchRisks(
            projectId: string,
            sprintId?: string,
            severity?: string,
            mitigationStatus?: string,
            page?: number,
            pageSize?: number
        ) {
            set({ loading: true, error: null });
            try {
                const { riskRegisterApi } = await import('@/api/riskRegisterApi');
                const result = await riskRegisterApi.getRisks({
                    projectId,
                    sprintId,
                    severity,
                    mitigationStatus,
                    page,
                    pageSize,
                });
                set({
                    risks: result.data,
                    totalCount: result.totalCount,
                    loading: false,
                });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch risks',
                });
            }
        },

        async createRisk(data: CreateRiskRequest) {
            set({ loading: true, error: null });
            try {
                const { riskRegisterApi } = await import('@/api/riskRegisterApi');
                await riskRegisterApi.createRisk(data);
                set({ loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to create risk',
                });
            }
        },

        async updateRisk(riskId: string, data: UpdateRiskRequest) {
            set({ loading: true, error: null });
            try {
                const { riskRegisterApi } = await import('@/api/riskRegisterApi');
                await riskRegisterApi.updateRisk(riskId, data);
                set({ loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to update risk',
                });
            }
        },

        async deleteRisk(riskId: string) {
            set({ loading: true, error: null });
            try {
                const { riskRegisterApi } = await import('@/api/riskRegisterApi');
                await riskRegisterApi.deleteRisk(riskId);
                set({ loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to delete risk',
                });
            }
        },

        reset() {
            set(initialState);
        },
    })
);
