import { create } from 'zustand';
import type { DependencyAnalysisResponse } from '@/types/analytics';

interface DependencyState {
    analysis: DependencyAnalysisResponse | null;
    loading: boolean;
    error: string | null;
}

interface DependencyActions {
    fetchDependencies(projectId: string, sprintId?: string): Promise<void>;
    reset(): void;
}

const initialState: DependencyState = {
    analysis: null,
    loading: false,
    error: null,
};

export const useDependencyStore = create<DependencyState & DependencyActions>()(
    (set) => ({
        ...initialState,

        async fetchDependencies(projectId: string, sprintId?: string) {
            set({ loading: true, error: null });
            try {
                const { analyticsApi } = await import('@/api/analyticsApi');
                const analysis = await analyticsApi.getDependencies({
                    projectId,
                    sprintId,
                });
                set({ analysis, loading: false });
            } catch (err) {
                set({
                    loading: false,
                    error: err instanceof Error ? err.message : 'Failed to fetch dependencies',
                });
            }
        },

        reset() {
            set(initialState);
        },
    })
);
