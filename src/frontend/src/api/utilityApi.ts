import { createApiClient } from './client';
import { env } from '@/utils/env';
import type { PaginatedResponse, PaginationParams } from '@/types/api';
import type {
    AuditLog,
    AuditLogFilters,
    NotificationLog,
    DepartmentType,
    PriorityLevel,
    TaskTypeRef,
    WorkflowState,
} from '@/types/utility';

const client = createApiClient({ baseURL: env.UTILITY_API_URL });

export const utilityApi = {
    getAuditLogs: (
        params?: PaginationParams & AuditLogFilters
    ): Promise<PaginatedResponse<AuditLog>> =>
        client.get('/api/v1/audit-logs', { params }).then((r) => r.data),

    getNotificationLogs: (
        params?: PaginationParams
    ): Promise<PaginatedResponse<NotificationLog>> =>
        client.get('/api/v1/notification-logs', { params }).then((r) => r.data),

    getReferenceData: (): Promise<{
        departmentTypes: DepartmentType[];
        priorityLevels: PriorityLevel[];
        taskTypes: TaskTypeRef[];
        workflowStates: WorkflowState[];
    }> => client.get('/api/v1/reference-data').then((r) => r.data),
};
