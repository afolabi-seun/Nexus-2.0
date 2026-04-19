import type {
    StoryStatus,
    TaskStatus,
    SprintStatus,
    Priority,
    TaskType,
    LinkType,
    FlgStatus,
} from './enums';

// --- Stories ---

export interface StoryListItem {
    storyId: string;
    projectId: string;
    projectName: string;
    storyKey: string;
    title: string;
    status: StoryStatus;
    priority: Priority;
    storyPoints: number | null;
    assigneeName: string | null;
    sprintName: string | null;
    departmentName: string | null;
    labels: Label[];
    dueDate: string | null;
    dateCreated: string;
}

export interface StoryDetail {
    storyId: string;
    projectId: string;
    projectName: string;
    projectKey: string;
    storyKey: string;
    sequenceNumber: number;
    title: string;
    description: string | null;
    acceptanceCriteria: string | null;
    storyPoints: number | null;
    priority: Priority;
    status: StoryStatus;
    assigneeId: string | null;
    assigneeName: string | null;
    assigneeAvatarUrl: string | null;
    reporterId: string;
    reporterName: string | null;
    sprintId: string | null;
    sprintName: string | null;
    departmentId: string | null;
    departmentName: string | null;
    dueDate: string | null;
    completedDate: string | null;
    totalTaskCount: number;
    completedTaskCount: number;
    completionPercentage: number;
    departmentContributions: DepartmentContribution[];
    tasks: TaskDetail[];
    labels: Label[];
    links: StoryLink[];
    commentCount: number;
    flgStatus: FlgStatus;
    dateCreated: string;
    dateUpdated: string;
}

export interface DepartmentContribution {
    departmentName: string;
    taskCount: number;
    completedTaskCount: number;
}

export interface StoryLink {
    linkId: string;
    targetStoryId: string;
    targetStoryKey: string;
    targetStoryTitle: string;
    linkType: LinkType;
}

export interface CreateStoryRequest {
    projectId: string;
    title: string;
    description?: string;
    acceptanceCriteria?: string;
    priority?: Priority;
    storyPoints?: number;
    departmentId?: string;
    dueDate?: string;
    labelIds?: string[];
}

export interface UpdateStoryRequest {
    title: string;
    description?: string;
    acceptanceCriteria?: string;
    priority?: Priority;
    storyPoints?: number;
    departmentId?: string;
    dueDate?: string;
}

export interface StoryStatusRequest {
    status: StoryStatus;
}

export interface StoryAssignRequest {
    assigneeId: string;
}

export interface CreateStoryLinkRequest {
    targetStoryId: string;
    linkType: LinkType;
}

export interface StoryFilters {
    projectId?: string;
    status?: string[];
    priority?: string[];
    departmentId?: string;
    assigneeId?: string;
    sprintId?: string;
    labelIds?: string[];
    dateFrom?: string;
    dateTo?: string;
}

// --- Tasks ---

export interface TaskDetail {
    taskId: string;
    storyId: string;
    storyKey: string;
    title: string;
    description: string | null;
    taskType: TaskType;
    status: TaskStatus;
    priority: Priority;
    assigneeId: string | null;
    assigneeName: string | null;
    departmentId: string | null;
    departmentName: string | null;
    estimatedHours: number | null;
    actualHours: number | null;
    dueDate: string | null;
    completedDate: string | null;
    flgStatus: FlgStatus;
    dateCreated: string;
    dateUpdated: string;
}

export interface CreateTaskRequest {
    storyId: string;
    title: string;
    description?: string;
    taskType: TaskType;
    priority?: Priority;
    estimatedHours?: number;
    dueDate?: string;
}

export interface LogHoursRequest {
    hours: number;
    description?: string;
}

export interface TaskStatusRequest {
    status: TaskStatus;
}

export interface TaskAssignRequest {
    assigneeId: string;
}

export interface UpdateTaskRequest {
    title?: string;
    description?: string;
    taskType?: TaskType;
    priority?: Priority;
    estimatedHours?: number;
    dueDate?: string;
}

export interface SuggestAssigneeResponse {
    memberId: string;
    memberName: string;
    reason: string;
}

// --- Labels ---

export interface Label {
    labelId: string;
    name: string;
    color: string;
}

export interface CreateLabelRequest {
    name: string;
    color: string;
}

export interface UpdateLabelRequest {
    name: string;
    color: string;
}

export interface ApplyLabelRequest {
    labelId: string;
}

// --- Sprints ---

export interface SprintListItem {
    sprintId: string;
    projectId: string;
    projectName: string;
    name: string;
    goal: string | null;
    status: SprintStatus;
    startDate: string;
    endDate: string;
    storyCount: number;
    velocity: number | null;
}

export interface SprintDetail extends SprintListItem {
    stories: StoryListItem[];
    totalStoryPoints: number;
    completedStoryPoints: number;
}

export interface CreateSprintRequest {
    name: string;
    goal?: string;
    startDate: string;
    endDate: string;
}

export interface UpdateSprintRequest {
    name: string;
    goal?: string;
    startDate: string;
    endDate: string;
}

export interface SprintFilters {
    projectId?: string;
    status?: string;
}

export interface SprintMetrics {
    totalStories: number;
    completedStories: number;
    totalStoryPoints: number;
    completedStoryPoints: number;
    completionRate: number;
    velocity: number;
    storiesByStatus: Record<string, number>;
    tasksByDepartment: Record<string, number>;
    burndownData: BurndownDataPoint[];
}

export interface BurndownDataPoint {
    date: string;
    idealRemaining: number;
    actualRemaining: number;
}

export interface VelocityChartData {
    sprintName: string;
    velocity: number;
    totalStoryPoints: number;
    completionRate: number;
    startDate: string;
    endDate: string;
}

export interface AddStoryToSprintRequest {
    storyId: string;
}

// --- Boards ---

export interface KanbanBoard {
    columns: KanbanColumn[];
}

export interface KanbanColumn {
    status: StoryStatus;
    cardCount: number;
    totalPoints: number;
    cards: KanbanCard[];
}

export interface KanbanCard {
    storyId: string;
    storyKey: string;
    title: string;
    priority: Priority;
    storyPoints: number | null;
    assigneeName: string | null;
    assigneeAvatarUrl: string | null;
    labels: Label[];
    taskCount: number;
    completedTaskCount: number;
    projectName: string;
}

export interface SprintBoard {
    sprintName: string | null;
    hasActiveSprint: boolean;
    message: string | null;
    projectName: string | null;
    columns: SprintBoardColumn[];
}

export interface SprintBoardColumn {
    status: TaskStatus;
    cards: SprintBoardCard[];
}

export interface SprintBoardCard {
    taskId: string;
    storyKey: string;
    taskTitle: string;
    taskType: TaskType;
    assigneeName: string | null;
    departmentName: string | null;
    priority: Priority;
    projectName: string;
}

export interface DepartmentBoard {
    departments: DepartmentBoardGroup[];
}

export interface DepartmentBoardGroup {
    departmentName: string;
    taskCount: number;
    memberCount: number;
    tasksByStatus: Record<string, number>;
}

export interface Backlog {
    totalStories: number;
    totalPoints: number;
    items: BacklogItem[];
}

export interface BacklogItem {
    storyId: string;
    storyKey: string;
    title: string;
    priority: Priority;
    storyPoints: number | null;
    status: StoryStatus;
    assigneeName: string | null;
    labels: Label[];
    taskCount: number;
    dateCreated: string;
    projectName: string;
}

export interface BoardFilters {
    projectId?: string;
    departmentId?: string;
    assigneeId?: string;
    priority?: string;
    labelIds?: string[];
}

// --- Comments ---

export interface Comment {
    commentId: string;
    entityType: string;
    entityId: string;
    authorId: string;
    authorName: string;
    authorAvatarUrl: string | null;
    content: string;
    parentCommentId: string | null;
    isEdited: boolean;
    replies: Comment[];
    dateCreated: string;
    dateUpdated: string;
}

export interface CreateCommentRequest {
    entityType: 'Story' | 'Task';
    entityId: string;
    content: string;
    parentCommentId?: string;
}

// --- Activity ---

export interface ActivityLogEntry {
    activityLogId: string;
    entityType: string;
    entityId: string;
    storyKey: string | null;
    action: string;
    actorId: string;
    actorName: string;
    oldValue: string | null;
    newValue: string | null;
    description: string;
    dateCreated: string;
}

// --- Search ---

export interface SearchParams {
    query: string;
    entityType?: string;
    page?: number;
    pageSize?: number;
}

export interface SearchResponse {
    totalCount: number;
    page: number;
    pageSize: number;
    items: SearchResultItem[];
}

export interface SearchResultItem {
    id: string;
    entityType: string;
    storyKey: string | null;
    title: string;
    status: string;
    priority: string;
    assigneeName: string | null;
    departmentName: string | null;
    relevance: number;
}

// --- Saved Filters ---

export interface SavedFilter {
    savedFilterId: string;
    name: string;
    filters: string;
    dateCreated: string;
}

export interface CreateSavedFilterRequest {
    name: string;
    filters: string;
}

// --- Projects ---

export interface ProjectListItem {
    projectId: string;
    name: string;
    projectKey: string;
    description: string | null;
    storyCount: number;
    sprintCount: number;
    leadName: string | null;
    status: string;
}

export interface ProjectDetail extends ProjectListItem {
    leadId: string | null;
    dateCreated: string;
    dateUpdated: string;
}

export interface CreateProjectRequest {
    name: string;
    projectKey: string;
    description?: string;
    leadId?: string;
}

export interface UpdateProjectRequest {
    name: string;
    description?: string;
    leadId?: string;
}

// --- Reports ---

export interface ReportFilters {
    projectId?: string;
    departmentId?: string;
    dateFrom?: string;
    dateTo?: string;
}

export interface DepartmentWorkloadData {
    departmentName: string;
    tasksByType: Record<string, number>;
    totalTasks: number;
}

export interface CapacityUtilizationData {
    departmentName: string;
    members: { memberName: string; activeTasks: number; maxTasks: number }[];
}

export interface CycleTimeData {
    period: string;
    averageDays: number;
}

export interface TaskCompletionData {
    departmentName: string;
    completionsByType: Record<string, { completed: number; total: number }>;
}


export interface StoryTemplateResponse {
    storyTemplateId: string;
    name: string;
    description: string | null;
    defaultTitle: string | null;
    defaultDescription: string | null;
    defaultAcceptanceCriteria: string | null;
    defaultPriority: string;
    defaultStoryPoints: number | null;
    defaultLabels: string[] | null;
    defaultTaskTypes: string[] | null;
    dateCreated: string;
}

export interface CreateStoryTemplateRequest {
    name: string;
    description?: string;
    defaultTitle?: string;
    defaultDescription?: string;
    defaultAcceptanceCriteria?: string;
    defaultPriority?: string;
    defaultStoryPoints?: number;
    defaultLabels?: string[];
    defaultTaskTypes?: string[];
}
