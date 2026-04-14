import { createApiClient } from './client';
import { env } from '@/utils/env';
import type { PaginatedResponse, PaginationParams } from '@/types/api';
import type {
    Organization,
    OrganizationSettings,
    UpdateOrganizationSettingsRequest,
    TeamMember,
    TeamMemberDetail,
    UpdateTeamMemberRequest,
    ChangeRoleRequest,
    AddDepartmentRequest,
    MemberFilters,
    Department,
    CreateDepartmentRequest,
    DepartmentPreferences,
    DepartmentPreferencesRequest,
    Invite,
    CreateInviteRequest,
    InviteValidation,
    AcceptInviteRequest,
    Device,
    UserPreferences,
    UpdatePreferencesRequest,
    ResolvedPreferences,
    NotificationSetting,
    UpdateNotificationSettingRequest,
    CreateOrganizationRequest,
    ProvisionAdminRequest,
} from '@/types/profile';

const client = createApiClient({ baseURL: env.PROFILE_API_URL });

export const profileApi = {
    // Organization
    getOrganization: (id: string): Promise<Organization> =>
        client.get(`/api/v1/organizations/${id}`).then((r) => r.data),

    updateOrganization: (id: string, data: { name?: string }): Promise<Organization> =>
        client.put(`/api/v1/organizations/${id}`, data).then((r) => r.data),

    updateOrganizationStatus: (id: string, data: { status: string }): Promise<void> =>
        client.patch(`/api/v1/organizations/${id}/status`, data).then(() => undefined),

    getOrganizationSettings: (id: string): Promise<OrganizationSettings> =>
        client.get(`/api/v1/organizations/${id}/settings`).then((r) => r.data),

    updateOrganizationSettings: (
        id: string,
        data: UpdateOrganizationSettingsRequest
    ): Promise<OrganizationSettings> =>
        client.put(`/api/v1/organizations/${id}/settings`, data).then((r) => r.data),

    // Team Members
    getTeamMembers: (
        params?: PaginationParams & MemberFilters
    ): Promise<PaginatedResponse<TeamMember>> =>
        client.get('/api/v1/team-members', { params }).then((r) => r.data),

    getTeamMember: (id: string): Promise<TeamMemberDetail> =>
        client.get(`/api/v1/team-members/${id}`).then((r) => r.data),

    updateTeamMember: (
        id: string,
        data: UpdateTeamMemberRequest
    ): Promise<TeamMember> =>
        client.put(`/api/v1/team-members/${id}`, data).then((r) => r.data),

    changeRole: (
        memberId: string,
        deptId: string,
        data: ChangeRoleRequest
    ): Promise<void> =>
        client
            .patch(`/api/v1/team-members/${memberId}/departments/${deptId}/role`, data)
            .then(() => undefined),

    addToDepartment: (
        memberId: string,
        data: AddDepartmentRequest
    ): Promise<void> =>
        client
            .post(`/api/v1/team-members/${memberId}/departments`, data)
            .then(() => undefined),

    removeFromDepartment: (memberId: string, deptId: string): Promise<void> =>
        client
            .delete(`/api/v1/team-members/${memberId}/departments/${deptId}`)
            .then(() => undefined),

    // Departments
    getDepartments: (params?: PaginationParams): Promise<PaginatedResponse<Department>> =>
        client.get('/api/v1/departments', { params }).then((r) => r.data),

    getDepartment: (id: string): Promise<Department> =>
        client.get(`/api/v1/departments/${id}`).then((r) => r.data),

    createDepartment: (data: CreateDepartmentRequest): Promise<Department> =>
        client.post('/api/v1/departments', data).then((r) => r.data),

    updateDepartment: (id: string, data: { departmentName?: string; departmentCode?: string }): Promise<Department> =>
        client.put(`/api/v1/departments/${id}`, data).then((r) => r.data),

    updateDepartmentStatus: (id: string, data: { status: string }): Promise<void> =>
        client.patch(`/api/v1/departments/${id}/status`, data).then(() => undefined),

    getDepartmentPreferences: (id: string): Promise<DepartmentPreferences> =>
        client.get(`/api/v1/departments/${id}/preferences`).then((r) => r.data),

    updateDepartmentPreferences: (
        id: string,
        data: DepartmentPreferencesRequest
    ): Promise<DepartmentPreferences> =>
        client
            .put(`/api/v1/departments/${id}/preferences`, data)
            .then((r) => r.data),

    // Invites
    getInvites: (): Promise<Invite[]> =>
        client.get('/api/v1/invites').then((r) => r.data),

    createInvite: (data: CreateInviteRequest): Promise<Invite> =>
        client.post('/api/v1/invites', data).then((r) => r.data),

    cancelInvite: (id: string): Promise<void> =>
        client.delete(`/api/v1/invites/${id}`).then(() => undefined),

    validateInvite: (token: string): Promise<InviteValidation> =>
        client.get(`/api/v1/invites/${token}/validate`).then((r) => r.data),

    acceptInvite: (token: string, data: AcceptInviteRequest): Promise<void> =>
        client.post(`/api/v1/invites/${token}/accept`, data).then(() => undefined),

    // Devices
    getDevices: (): Promise<Device[]> =>
        client.get('/api/v1/devices').then((r) => r.data),

    removeDevice: (id: string): Promise<void> =>
        client.delete(`/api/v1/devices/${id}`).then(() => undefined),

    setPrimaryDevice: (id: string): Promise<void> =>
        client.patch(`/api/v1/devices/${id}/primary`).then(() => undefined),

    // Preferences
    getPreferences: (): Promise<UserPreferences> =>
        client.get('/api/v1/preferences').then((r) => r.data),

    updatePreferences: (
        data: UpdatePreferencesRequest
    ): Promise<UserPreferences> =>
        client.put('/api/v1/preferences', data).then((r) => r.data),

    getResolvedPreferences: (): Promise<ResolvedPreferences> =>
        client.get('/api/v1/preferences/resolved').then((r) => r.data),

    // Notification Settings
    getNotificationSettings: (): Promise<NotificationSetting[]> =>
        client.get('/api/v1/notification-settings').then((r) => r.data),

    updateNotificationSetting: (
        typeId: string,
        data: UpdateNotificationSettingRequest
    ): Promise<void> =>
        client
            .put(`/api/v1/notification-settings/${typeId}`, data)
            .then(() => undefined),

    // PlatformAdmin
    getAllOrganizations: (): Promise<Organization[]> =>
        client.get('/api/v1/admin/organizations').then((r) => r.data),

    createOrganization: (
        data: CreateOrganizationRequest
    ): Promise<Organization> =>
        client.post('/api/v1/admin/organizations', data).then((r) => r.data),

    provisionAdmin: (
        orgId: string,
        data: ProvisionAdminRequest
    ): Promise<void> =>
        client
            .post(`/api/v1/admin/organizations/${orgId}/provision`, data)
            .then(() => undefined),

    // Member Status & Availability
    updateMemberStatus: (id: string, data: { status: string }): Promise<void> =>
        client.patch(`/api/v1/team-members/${id}/status`, data).then(() => undefined),

    updateAvailability: (id: string, data: { availability: string }): Promise<void> =>
        client.patch(`/api/v1/team-members/${id}/availability`, data).then(() => undefined),

    // Navigation
    getNavigation: (): Promise<import('@/types/profile').NavigationItem[]> =>
        client.get('/api/v1/navigation').then((r) => r.data),

    // Roles
    getRoles: (): Promise<object[]> =>
        client.get('/api/v1/roles').then((r) => r.data),
    getRole: (id: string): Promise<object> =>
        client.get(`/api/v1/roles/${id}`).then((r) => r.data),

    // Notification Types
    getNotificationTypes: (): Promise<object[]> =>
        client.get('/api/v1/notification-types').then((r) => r.data),

    // Department Members
    getDepartmentMembers: (id: string, params?: PaginationParams): Promise<PaginatedResponse<TeamMember>> =>
        client.get(`/api/v1/departments/${id}/members`, { params }).then((r) => r.data),
};
