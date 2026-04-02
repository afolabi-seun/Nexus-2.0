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
    ErrorLog,
    ErrorLogFilters,
    CreateDepartmentTypeRequest,
    CreatePriorityLevelRequest,
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

    getArchivedAuditLogs: (
        params?: PaginationParams & AuditLogFilters
    ): Promise<PaginatedResponse<AuditLog>> =>
        client.get('/api/v1/audit-logs/archive', { params }).then((r) => r.data),

    getErrorLogs: (
        params?: PaginationParams & ErrorLogFilters
    ): Promise<PaginatedResponse<ErrorLog>> =>
        client.get('/api/v1/error-logs', { params }).then((r) => r.data),

    getDepartmentTypes: (): Promise<DepartmentType[]> =>
        client.get('/api/v1/reference/department-types').then((r) => r.data),

    getPriorityLevels: (): Promise<PriorityLevel[]> =>
        client.get('/api/v1/reference/priority-levels').then((r) => r.data),

    getTaskTypes: (): Promise<TaskTypeRef[]> =>
        client.get('/api/v1/reference/task-types').then((r) => r.data),

    getWorkflowStates: (): Promise<WorkflowState[]> =>
        client.get('/api/v1/reference/workflow-states').then((r) => r.data),

    createDepartmentType: (data: CreateDepartmentTypeRequest): Promise<DepartmentType> =>
        client.post('/api/v1/reference/department-types', data).then((r) => r.data),

    createPriorityLevel: (data: CreatePriorityLevelRequest): Promise<PriorityLevel> =>
        client.post('/api/v1/reference/priority-levels', data).then((r) => r.data),
};
