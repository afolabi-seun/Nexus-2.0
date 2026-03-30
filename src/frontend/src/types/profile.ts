import type {
    FlgStatus,
    Availability,
    Theme,
    BoardView,
    DigestFrequency,
    DateFormat,
    TimeFormat,
} from './enums';

export interface Organization {
    organizationId: string;
    name: string;
    description: string | null;
    website: string | null;
    storyIdPrefix: string;
    timeZone: string;
    logoUrl: string | null;
    flgStatus: FlgStatus;
    memberCount: number;
    dateCreated: string;
}

export interface OrganizationSettings {
    storyPointScale: string | null;
    requiredFieldsByStoryType: Record<string, string[]> | null;
    autoAssignmentEnabled: boolean;
    autoAssignmentStrategy: string | null;
    workingDays: string[] | null;
    workingHoursStart: string | null;
    workingHoursEnd: string | null;
    primaryColor: string | null;
    defaultBoardView: string | null;
    wipLimitsEnabled: boolean;
    defaultWipLimit: number;
    defaultNotificationChannels: string | null;
    digestFrequency: string | null;
    auditRetentionDays: number;
}

export interface UpdateOrganizationSettingsRequest {
    storyPointScale?: string;
    autoAssignmentEnabled?: boolean;
    autoAssignmentStrategy?: string;
    workingDays?: string[];
    workingHoursStart?: string;
    workingHoursEnd?: string;
    primaryColor?: string;
    defaultBoardView?: string;
    wipLimitsEnabled?: boolean;
    defaultWipLimit?: number;
    defaultNotificationChannels?: string;
    digestFrequency?: string;
    auditRetentionDays?: number;
}

export interface Department {
    departmentId: string;
    name: string;
    code: string;
    description: string | null;
    memberCount: number;
    isDefault: boolean;
    flgStatus: FlgStatus;
}

export interface CreateDepartmentRequest {
    name: string;
    code: string;
    description?: string;
}

export interface DepartmentPreferences {
    defaultTaskTypes: string[] | null;
    wipLimitPerStatus: Record<string, number> | null;
    defaultAssigneeId: string | null;
    notificationChannelOverrides: Record<string, boolean> | null;
    maxConcurrentTasksDefault: number;
}

export interface DepartmentPreferencesRequest {
    defaultTaskTypes?: string[];
    wipLimitPerStatus?: Record<string, number>;
    defaultAssigneeId?: string;
    notificationChannelOverrides?: Record<string, boolean>;
    maxConcurrentTasksDefault?: number;
}

export interface TeamMember {
    teamMemberId: string;
    professionalId: string;
    firstName: string;
    lastName: string;
    email: string;
    avatarUrl: string | null;
    departmentName: string | null;
    roleName: string | null;
    availability: Availability;
    flgStatus: FlgStatus;
}

export interface TeamMemberDetail extends TeamMember {
    skills: string[] | null;
    maxConcurrentTasks: number;
    activeTaskCount: number;
    departmentMemberships: DepartmentMembership[];
    dateCreated: string;
    dateUpdated: string;
}

export interface DepartmentMembership {
    departmentId: string;
    departmentName: string;
    roleName: string;
    roleLevel: number;
}

export interface UpdateTeamMemberRequest {
    availability?: Availability;
    maxConcurrentTasks?: number;
    skills?: string[];
}

export interface ChangeRoleRequest {
    roleId: string;
}

export interface AddDepartmentRequest {
    departmentId: string;
    roleId: string;
}

export interface MemberFilters {
    departmentId?: string;
    roleName?: string;
    status?: string;
    availability?: string;
}

export interface Invite {
    inviteId: string;
    email: string;
    firstName: string;
    lastName: string;
    departmentName: string;
    roleName: string;
    expiryDate: string;
    status: string;
}

export interface CreateInviteRequest {
    email: string;
    firstName: string;
    lastName: string;
    departmentId: string;
    roleId: string;
}

export interface InviteValidation {
    organizationName: string;
    departmentName: string;
    roleName: string;
}

export interface AcceptInviteRequest {
    otp: string;
}

export interface Device {
    deviceId: string;
    deviceName: string;
    deviceType: string;
    isPrimary: boolean;
    ipAddress: string | null;
    lastActiveDate: string;
}

export interface UserPreferences {
    theme: Theme;
    language: string;
    timezoneOverride: string | null;
    defaultBoardView: BoardView | null;
    defaultBoardFilters: unknown | null;
    dashboardLayout: unknown | null;
    emailDigestFrequency: DigestFrequency | null;
    keyboardShortcutsEnabled: boolean;
    dateFormat: DateFormat;
    timeFormat: TimeFormat;
}

export interface ResolvedPreferences {
    theme: string;
    language: string;
    timezone: string;
    defaultBoardView: string;
    digestFrequency: string;
    notificationChannels: string;
    keyboardShortcutsEnabled: boolean;
    dateFormat: string;
    timeFormat: string;
    storyPointScale: string;
    autoAssignmentEnabled: boolean;
    autoAssignmentStrategy: string;
    wipLimitsEnabled: boolean;
    defaultWipLimit: number;
    auditRetentionDays: number;
    maxConcurrentTasksDefault: number;
}

export interface UpdatePreferencesRequest {
    theme?: Theme;
    language?: string;
    timezoneOverride?: string;
    defaultBoardView?: BoardView;
    emailDigestFrequency?: DigestFrequency;
    keyboardShortcutsEnabled?: boolean;
    dateFormat?: DateFormat;
    timeFormat?: TimeFormat;
}

export interface NotificationSetting {
    notificationTypeId: string;
    typeName: string;
    emailEnabled: boolean;
    pushEnabled: boolean;
    inAppEnabled: boolean;
}

export interface UpdateNotificationSettingRequest {
    emailEnabled: boolean;
    pushEnabled: boolean;
    inAppEnabled: boolean;
}

export interface CreateOrganizationRequest {
    name: string;
    description?: string;
    website?: string;
    storyIdPrefix: string;
}

export interface ProvisionAdminRequest {
    email: string;
    firstName: string;
    lastName: string;
}

// Navigation
export interface NavigationItem {
    navigationItemId: string;
    label: string;
    path: string;
    icon: string;
    sortOrder: number;
    parentId: string | null;
    minPermissionLevel: number;
    isEnabled: boolean;
    children: NavigationItem[];
}
