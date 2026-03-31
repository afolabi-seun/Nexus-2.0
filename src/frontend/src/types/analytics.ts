// ── Velocity ──

export interface VelocitySnapshotResponse {
    sprintId: string;
    sprintName: string;
    startDate: string;
    endDate: string;
    committedPoints: number;
    completedPoints: number;
    totalLoggedHours: number;
    averageHoursPerPoint: number | null;
    completedStoryCount: number;
}

// ── Resource Management ──

export interface ProjectBreakdownItem {
    projectId: string;
    projectName: string;
    hoursLogged: number;
    percentage: number;
}

export interface ResourceManagementResponse {
    memberId: string;
    memberName: string;
    departmentId: string;
    totalLoggedHours: number;
    projectBreakdown: ProjectBreakdownItem[];
    capacityUtilizationPercentage: number;
}

// ── Resource Utilization ──

export interface ResourceUtilizationDetailResponse {
    memberId: string;
    memberName: string;
    totalLoggedHours: number;
    expectedHours: number;
    utilizationPercentage: number;
    billableHours: number;
    nonBillableHours: number;
    overtimeHours: number;
}

// ── Project Cost ──

export interface MemberCostDetail {
    memberId: string;
    memberName: string;
    hours: number;
    cost: number;
}

export interface DepartmentCostDetail {
    departmentId: string;
    departmentName: string;
    hours: number;
    cost: number;
}

export interface CostTrendItem {
    snapshotDate: string;
    totalCost: number;
}

export interface ProjectCostAnalyticsResponse {
    totalCost: number;
    totalBillableHours: number;
    totalNonBillableHours: number;
    burnRatePerDay: number;
    costByMember: MemberCostDetail[];
    costByDepartment: DepartmentCostDetail[];
    costTrend: CostTrendItem[];
}

// ── Project Health ──

export interface ProjectHealthResponse {
    overallScore: number;
    velocityScore: number;
    bugRateScore: number;
    overdueScore: number;
    riskScore: number;
    trend: string;
    snapshotDate: string;
    history?: ProjectHealthResponse[] | null;
}

// ── Dependencies ──

export interface ChainStoryDetail {
    storyId: string;
    storyKey: string;
    title: string;
    status: string;
    assigneeId: string | null;
}

export interface DependencyChain {
    chainLength: number;
    stories: ChainStoryDetail[];
    criticalPath: boolean;
}

export interface BlockedStoryDetail {
    storyId: string;
    storyKey: string;
    title: string;
    status: string;
    blockedByStoryIds: string[];
}

export interface DependencyAnalysisResponse {
    totalDependencies: number;
    blockingChains: DependencyChain[];
    blockedStories: BlockedStoryDetail[];
    circularDependencies: string[][];
}

// ── Bug Metrics ──

export interface BugTrendItem {
    sprintId: string;
    sprintName: string;
    bugCount: number;
}

export interface BugMetricsResponse {
    totalBugs: number;
    openBugs: number;
    closedBugs: number;
    reopenedBugs: number;
    bugRate: number;
    bugsBySeverity: Record<string, number>;
    bugTrend: BugTrendItem[];
}

// ── Dashboard ──

export interface DashboardSummaryResponse {
    projectHealth: ProjectHealthResponse | null;
    velocitySnapshot: VelocitySnapshotResponse | null;
    activeBugCount: number;
    activeRiskCount: number;
    blockedStoryCount: number;
    totalProjectCost: number;
    burnRatePerDay: number;
}

// ── Snapshot Status ──

export interface SnapshotStatusResponse {
    lastRunTime: string | null;
    projectsProcessed: number;
    errorsEncountered: number;
    nextScheduledRun: string | null;
}

// ── Risk Register ──

export interface RiskRegisterResponse {
    riskRegisterId: string;
    organizationId: string;
    projectId: string;
    sprintId: string | null;
    title: string;
    description: string | null;
    severity: string;
    likelihood: string;
    mitigationStatus: string;
    createdBy: string;
    flgStatus: string;
    dateCreated: string;
    dateUpdated: string;
}

export interface CreateRiskRequest {
    projectId: string;
    sprintId?: string | null;
    title: string;
    description?: string | null;
    severity: string;
    likelihood: string;
    mitigationStatus: string;
}

export interface UpdateRiskRequest {
    title?: string | null;
    description?: string | null;
    severity?: string | null;
    likelihood?: string | null;
    mitigationStatus?: string | null;
}
