// ── Time Entries ──

export interface CreateTimeEntryRequest {
    storyId: string;
    date: string;
    hours: number;
    description?: string | null;
    billable?: boolean;
}

export interface UpdateTimeEntryRequest {
    date?: string;
    hours?: number;
    description?: string | null;
    billable?: boolean;
}

export interface TimeEntryResponse {
    timeEntryId: string;
    organizationId: string;
    memberId: string;
    memberName: string;
    storyId: string;
    storyKey: string;
    storyTitle: string;
    projectId: string;
    projectName: string;
    sprintId: string | null;
    sprintName: string | null;
    date: string;
    hours: number;
    description: string | null;
    billable: boolean;
    status: string;
    approvedBy: string | null;
    rejectedBy: string | null;
    rejectionReason: string | null;
    dateCreated: string;
    dateUpdated: string;
}

export interface RejectTimeEntryRequest {
    reason: string;
}

// ── Timer ──

export interface TimerStartRequest {
    storyId: string;
}

export interface TimerStatusResponse {
    isRunning: boolean;
    storyId: string | null;
    storyKey: string | null;
    storyTitle: string | null;
    startedAt: string | null;
    elapsedSeconds: number;
}

// ── Cost Rates ──

export interface CreateCostRateRequest {
    rateType: string;
    memberId?: string | null;
    departmentId?: string | null;
    hourlyRate: number;
    currency?: string;
    effectiveFrom: string;
    effectiveTo?: string | null;
}

export interface UpdateCostRateRequest {
    hourlyRate?: number;
    currency?: string;
    effectiveFrom?: string;
    effectiveTo?: string | null;
}

export interface CostRateResponse {
    costRateId: string;
    organizationId: string;
    rateType: string;
    memberId: string | null;
    memberName: string | null;
    departmentId: string | null;
    departmentName: string | null;
    hourlyRate: number;
    currency: string;
    effectiveFrom: string;
    effectiveTo: string | null;
    dateCreated: string;
    dateUpdated: string;
}

// ── Time Policy ──

export interface UpdateTimePolicyRequest {
    requiredHoursPerDay?: number;
    overtimeThreshold?: number;
    approvalRequired?: boolean;
    approvalWorkflow?: string;
    maxDailyHours?: number;
}

export interface TimePolicyResponse {
    timePolicyId: string;
    organizationId: string;
    requiredHoursPerDay: number;
    overtimeThreshold: number;
    approvalRequired: boolean;
    approvalWorkflow: string;
    maxDailyHours: number;
    dateCreated: string;
    dateUpdated: string;
}
