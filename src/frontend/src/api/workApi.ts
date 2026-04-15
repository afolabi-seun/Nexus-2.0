import { createApiClient } from './client';
import { env } from '@/utils/env';
import type { PaginatedResponse, PaginationParams } from '@/types/api';
import type {
    ProjectListItem,
    ProjectDetail,
    CreateProjectRequest,
    UpdateProjectRequest,
    StoryListItem,
    StoryDetail,
    CreateStoryRequest,
    UpdateStoryRequest,
    StoryStatusRequest,
    StoryAssignRequest,
    StoryFilters,
    ActivityLogEntry,
    Label,
    CreateLabelRequest,
    UpdateLabelRequest,
    ApplyLabelRequest,
    CreateStoryLinkRequest,
    TaskDetail,
    CreateTaskRequest,
    UpdateTaskRequest,
    TaskStatusRequest,
    TaskAssignRequest,
    LogHoursRequest,
    SuggestAssigneeResponse,
    SprintListItem,
    SprintDetail,
    CreateSprintRequest,
    UpdateSprintRequest,
    SprintFilters,
    SprintMetrics,
    VelocityChartData,
    AddStoryToSprintRequest,
    KanbanBoard,
    SprintBoard,
    DepartmentBoard,
    Backlog,
    BoardFilters,
    Comment,
    CreateCommentRequest,
    SearchParams,
    SearchResponse,
    SavedFilter,
    CreateSavedFilterRequest,
    ReportFilters,
    DepartmentWorkloadData,
    CapacityUtilizationData,
    CycleTimeData,
    TaskCompletionData,
} from '@/types/work';

const client = createApiClient({ baseURL: env.WORK_API_URL });

export const workApi = {
    // Projects
    getProjects: (params?: PaginationParams): Promise<PaginatedResponse<ProjectListItem>> =>
        client.get('/api/v1/projects', { params }).then((r) => r.data),
    getProject: (id: string): Promise<ProjectDetail> =>
        client.get(`/api/v1/projects/${id}`).then((r) => r.data),
    createProject: (data: CreateProjectRequest): Promise<ProjectDetail> =>
        client.post('/api/v1/projects', data).then((r) => r.data),
    updateProject: (id: string, data: UpdateProjectRequest): Promise<ProjectDetail> =>
        client.put(`/api/v1/projects/${id}`, data).then((r) => r.data),
    updateProjectStatus: (id: string, data: { status: string }): Promise<void> =>
        client.patch(`/api/v1/projects/${id}/status`, data).then(() => undefined),
    getProjectCostSummary: (id: string, params?: { dateFrom?: string; dateTo?: string }): Promise<object> =>
        client.get(`/api/v1/projects/${id}/cost-summary`, { params }).then((r) => r.data),
    getProjectUtilization: (id: string, params?: { dateFrom?: string; dateTo?: string }): Promise<object> =>
        client.get(`/api/v1/projects/${id}/utilization`, { params }).then((r) => r.data),
    exportStoriesCsv: (params?: { projectId?: string; sprintId?: string }): Promise<Blob> =>
        client.get('/api/v1/projects/export/stories', { params, responseType: 'blob' }).then((r) => r.data),
    exportTimeEntriesCsv: (params?: { projectId?: string; dateFrom?: string; dateTo?: string }): Promise<Blob> =>
        client.get('/api/v1/projects/export/time-entries', { params, responseType: 'blob' }).then((r) => r.data),

    // Stories
    getStories: (params?: PaginationParams & StoryFilters): Promise<PaginatedResponse<StoryListItem>> =>
        client.get('/api/v1/stories', { params }).then((r) => r.data),
    getStory: (id: string): Promise<StoryDetail> =>
        client.get(`/api/v1/stories/${id}`).then((r) => r.data),
    getStoryByKey: (key: string): Promise<StoryDetail> =>
        client.get(`/api/v1/stories/by-key/${key}`).then((r) => r.data),
    createStory: (data: CreateStoryRequest): Promise<StoryDetail> =>
        client.post('/api/v1/stories', data).then((r) => r.data),
    updateStory: (id: string, data: UpdateStoryRequest): Promise<StoryDetail> =>
        client.put(`/api/v1/stories/${id}`, data).then((r) => r.data),
    updateStoryStatus: (id: string, data: StoryStatusRequest): Promise<void> =>
        client.patch(`/api/v1/stories/${id}/status`, data).then(() => undefined),
    assignStory: (id: string, data: StoryAssignRequest): Promise<void> =>
        client.patch(`/api/v1/stories/${id}/assign`, data).then(() => undefined),
    getStoryActivity: (id: string): Promise<ActivityLogEntry[]> =>
        client.get(`/api/v1/stories/${id}/activity`).then((r) => r.data),
    deleteStory: (id: string): Promise<void> =>
        client.delete(`/api/v1/stories/${id}`).then(() => undefined),
    unassignStory: (id: string): Promise<void> =>
        client.patch(`/api/v1/stories/${id}/unassign`).then(() => undefined),

    // Bulk Operations
    bulkUpdateStatus: (data: { storyIds: string[]; status: string }): Promise<object> =>
        client.post('/api/v1/stories/bulk/status', data).then((r) => r.data),
    bulkAssign: (data: { storyIds: string[]; assigneeId: string }): Promise<object> =>
        client.post('/api/v1/stories/bulk/assign', data).then((r) => r.data),

    // Activity Feed
    getActivityFeed: (params?: PaginationParams): Promise<PaginatedResponse<ActivityLogEntry>> =>
        client.get('/api/v1/activity-feed', { params }).then((r) => r.data),

    // Story Labels
    getLabels: (): Promise<Label[]> =>
        client.get('/api/v1/labels').then((r) => r.data),
    createLabel: (data: CreateLabelRequest): Promise<Label> =>
        client.post('/api/v1/labels', data).then((r) => r.data),
    updateLabel: (id: string, data: UpdateLabelRequest): Promise<Label> =>
        client.put(`/api/v1/labels/${id}`, data).then((r) => r.data),
    deleteLabel: (id: string): Promise<void> =>
        client.delete(`/api/v1/labels/${id}`).then(() => undefined),
    applyLabel: (storyId: string, data: ApplyLabelRequest): Promise<void> =>
        client.post(`/api/v1/stories/${storyId}/labels`, data).then(() => undefined),
    removeLabel: (storyId: string, labelId: string): Promise<void> =>
        client.delete(`/api/v1/stories/${storyId}/labels/${labelId}`).then(() => undefined),

    // Story Links
    createStoryLink: (storyId: string, data: CreateStoryLinkRequest): Promise<void> =>
        client.post(`/api/v1/stories/${storyId}/links`, data).then(() => undefined),
    removeStoryLink: (storyId: string, linkId: string): Promise<void> =>
        client.delete(`/api/v1/stories/${storyId}/links/${linkId}`).then(() => undefined),

    // Tasks
    createTask: (data: CreateTaskRequest): Promise<TaskDetail> =>
        client.post('/api/v1/tasks', data).then((r) => r.data),
    updateTask: (id: string, data: UpdateTaskRequest): Promise<TaskDetail> =>
        client.put(`/api/v1/tasks/${id}`, data).then((r) => r.data),
    updateTaskStatus: (id: string, data: TaskStatusRequest): Promise<void> =>
        client.patch(`/api/v1/tasks/${id}/status`, data).then(() => undefined),
    assignTask: (id: string, data: TaskAssignRequest): Promise<void> =>
        client.patch(`/api/v1/tasks/${id}/assign`, data).then(() => undefined),
    selfAssignTask: (id: string): Promise<void> =>
        client.patch(`/api/v1/tasks/${id}/self-assign`).then(() => undefined),
    suggestAssignee: (params: { storyId: string; taskType: string }): Promise<SuggestAssigneeResponse> =>
        client.get('/api/v1/tasks/suggest-assignee', { params }).then((r) => r.data),
    logHours: (id: string, data: LogHoursRequest): Promise<void> =>
        client.patch(`/api/v1/tasks/${id}/log-hours`, data).then(() => undefined),
    getTaskActivity: (id: string): Promise<ActivityLogEntry[]> =>
        client.get(`/api/v1/tasks/${id}/activity`).then((r) => r.data),
    deleteTask: (id: string): Promise<void> =>
        client.delete(`/api/v1/tasks/${id}`).then(() => undefined),
    unassignTask: (id: string): Promise<void> =>
        client.patch(`/api/v1/tasks/${id}/unassign`).then(() => undefined),

    // Sprints
    getActiveSprint: (): Promise<SprintDetail> =>
        client.get('/api/v1/sprints/active').then((r) => r.data),
    getSprints: (params?: PaginationParams & SprintFilters): Promise<PaginatedResponse<SprintListItem>> =>
        client.get('/api/v1/sprints', { params }).then((r) => r.data),
    getSprint: (id: string): Promise<SprintDetail> =>
        client.get(`/api/v1/sprints/${id}`).then((r) => r.data),
    createSprint: (projectId: string, data: CreateSprintRequest): Promise<SprintDetail> =>
        client.post(`/api/v1/projects/${projectId}/sprints`, data).then((r) => r.data),
    startSprint: (id: string): Promise<void> =>
        client.patch(`/api/v1/sprints/${id}/start`).then(() => undefined),
    completeSprint: (id: string): Promise<void> =>
        client.patch(`/api/v1/sprints/${id}/complete`).then(() => undefined),
    cancelSprint: (id: string): Promise<void> =>
        client.patch(`/api/v1/sprints/${id}/cancel`).then(() => undefined),
    getSprintMetrics: (id: string): Promise<SprintMetrics> =>
        client.get(`/api/v1/sprints/${id}/metrics`).then((r) => r.data),
    getVelocity: (params: { count: number }): Promise<VelocityChartData[]> =>
        client.get('/api/v1/sprints/velocity', { params }).then((r) => r.data),
    addStoryToSprint: (sprintId: string, data: AddStoryToSprintRequest): Promise<void> =>
        client.post(`/api/v1/sprints/${sprintId}/stories`, data).then(() => undefined),
    removeStoryFromSprint: (sprintId: string, storyId: string): Promise<void> =>
        client.delete(`/api/v1/sprints/${sprintId}/stories/${storyId}`).then(() => undefined),
    updateSprint: (id: string, data: UpdateSprintRequest): Promise<SprintDetail> =>
        client.put(`/api/v1/sprints/${id}`, data).then((r) => r.data),

    // Boards
    getKanbanBoard: (params?: BoardFilters): Promise<KanbanBoard> =>
        client.get('/api/v1/boards/kanban', { params }).then((r) => r.data),
    getSprintBoard: (params?: BoardFilters): Promise<SprintBoard> =>
        client.get('/api/v1/boards/sprint', { params }).then((r) => r.data),
    getDepartmentBoard: (params?: BoardFilters): Promise<DepartmentBoard> =>
        client.get('/api/v1/boards/department', { params }).then((r) => r.data),
    getBacklog: (params?: BoardFilters): Promise<Backlog> =>
        client.get('/api/v1/boards/backlog', { params }).then((r) => r.data),

    // Comments
    getComments: (entityType: string, entityId: string): Promise<Comment[]> =>
        client.get(`/api/v1/comments`, { params: { entityType, entityId } }).then((r) => r.data),
    createComment: (data: CreateCommentRequest): Promise<Comment> =>
        client.post('/api/v1/comments', data).then((r) => r.data),
    updateComment: (id: string, data: { content: string }): Promise<Comment> =>
        client.put(`/api/v1/comments/${id}`, data).then((r) => r.data),
    deleteComment: (id: string): Promise<void> =>
        client.delete(`/api/v1/comments/${id}`).then(() => undefined),

    // Search
    search: (params: SearchParams): Promise<SearchResponse> =>
        client.get('/api/v1/search', { params }).then((r) => r.data),

    // Saved Filters
    getSavedFilters: (): Promise<SavedFilter[]> =>
        client.get('/api/v1/saved-filters').then((r) => r.data),
    createSavedFilter: (data: CreateSavedFilterRequest): Promise<SavedFilter> =>
        client.post('/api/v1/saved-filters', data).then((r) => r.data),
    deleteSavedFilter: (id: string): Promise<void> =>
        client.delete(`/api/v1/saved-filters/${id}`).then(() => undefined),

    // Reports
    getVelocityReport: (params?: ReportFilters): Promise<VelocityChartData[]> =>
        client.get('/api/v1/reports/velocity', { params }).then((r) => r.data),
    getDepartmentWorkloadReport: (params?: ReportFilters): Promise<DepartmentWorkloadData[]> =>
        client.get('/api/v1/reports/department-workload', { params }).then((r) => r.data),
    getCapacityReport: (params?: ReportFilters): Promise<CapacityUtilizationData[]> =>
        client.get('/api/v1/reports/capacity', { params }).then((r) => r.data),
    getCycleTimeReport: (params?: ReportFilters): Promise<CycleTimeData[]> =>
        client.get('/api/v1/reports/cycle-time', { params }).then((r) => r.data),
    getTaskCompletionReport: (params?: ReportFilters): Promise<TaskCompletionData[]> =>
        client.get('/api/v1/reports/task-completion', { params }).then((r) => r.data),

    // Workflows
    getWorkflows: (): Promise<object> =>
        client.get('/api/v1/workflows').then((r) => r.data),
    saveOrgWorkflowOverride: (data: object): Promise<void> =>
        client.put('/api/v1/workflows/organization', data).then(() => undefined),
    saveDeptWorkflowOverride: (departmentId: string, data: object): Promise<void> =>
        client.put(`/api/v1/workflows/department/${departmentId}`, data).then(() => undefined),

    // Sprint Velocity (per-sprint)
    getSprintVelocity: (sprintId: string): Promise<object> =>
        client.get(`/api/v1/sprints/${sprintId}/velocity`).then((r) => r.data),

    // Cost Snapshots
    getProjectCostSnapshots: (projectId: string, params?: PaginationParams & { dateFrom?: string; dateTo?: string }): Promise<PaginatedResponse<object>> =>
        client.get(`/api/v1/projects/${projectId}/cost-snapshots`, { params }).then((r) => r.data),
};
