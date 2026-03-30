export interface AuditLog {
    auditLogId: string;
    action: string;
    entityType: string;
    entityId: string;
    actorId: string;
    actorName: string;
    details: string | null;
    ipAddress: string | null;
    dateCreated: string;
}

export interface AuditLogFilters {
    action?: string;
    entityType?: string;
    actorId?: string;
    dateFrom?: string;
    dateTo?: string;
}

export interface NotificationLog {
    notificationLogId: string;
    notificationType: string;
    channel: string;
    subject: string;
    recipientId: string;
    status: string;
    dateCreated: string;
}

export interface DepartmentType {
    code: string;
    name: string;
}

export interface PriorityLevel {
    code: string;
    name: string;
    level: number;
}

export interface TaskTypeRef {
    code: string;
    name: string;
    defaultDepartment: string;
}

export interface WorkflowState {
    entityType: string;
    status: string;
    validTransitions: string[];
}

export interface ReferenceData {
    departmentTypes: DepartmentType[];
    priorityLevels: PriorityLevel[];
    taskTypes: TaskTypeRef[];
    workflowStates: WorkflowState[];
}
