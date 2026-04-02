import { createBrowserRouter } from 'react-router-dom';

// Guards
import { AuthGuard } from '@/components/guards/AuthGuard';
import { GuestGuard } from '@/components/guards/GuestGuard';
import { RoleGuard } from '@/components/guards/RoleGuard';
import { OrgUserGuard } from '@/components/guards/OrgUserGuard';
import { FirstTimeGuard } from '@/components/guards/FirstTimeGuard';

// Layouts
import { AppShell } from '@/components/layout/AppShell';
import { AuthLayout } from '@/components/layout/AuthLayout';
import { AdminLayout } from '@/components/layout/AdminLayout';

// Pages — Auth
import { LoginPage } from '@/features/auth/pages/LoginPage';
import { PlatformAdminLoginPage } from '@/features/auth/pages/PlatformAdminLoginPage';
import { ForcedPasswordChangePage } from '@/features/auth/pages/ForcedPasswordChangePage';
import { PasswordResetPage } from '@/features/auth/pages/PasswordResetPage';

// Pages — App
import { DashboardPage } from '@/features/dashboard/pages/DashboardPage';
import { ProjectListPage } from '@/features/projects/pages/ProjectListPage';
import { ProjectDetailPage } from '@/features/projects/pages/ProjectDetailPage';
import { StoryListPage } from '@/features/stories/pages/StoryListPage';
import { StoryDetailPage } from '@/features/stories/pages/StoryDetailPage';
import { StoryByKeyRedirect } from '@/features/stories/pages/StoryByKeyRedirect';
import { KanbanBoardPage } from '@/features/boards/pages/KanbanBoardPage';
import { SprintBoardPage } from '@/features/boards/pages/SprintBoardPage';
import { DepartmentBoardPage } from '@/features/boards/pages/DepartmentBoardPage';
import { BacklogPage } from '@/features/boards/pages/BacklogPage';
import { SprintListPage } from '@/features/sprints/pages/SprintListPage';
import { SprintDetailPage } from '@/features/sprints/pages/SprintDetailPage';
import { MemberListPage } from '@/features/members/pages/MemberListPage';
import { MemberProfilePage } from '@/features/members/pages/MemberProfilePage';
import { DepartmentListPage } from '@/features/departments/pages/DepartmentListPage';
import { DepartmentDetailPage } from '@/features/departments/pages/DepartmentDetailPage';
import { SettingsPage } from '@/features/settings/pages/SettingsPage';
import { PreferencesPage } from '@/features/preferences/pages/PreferencesPage';
import { InviteManagementPage } from '@/features/invites/pages/InviteManagementPage';
import { AcceptInvitePage } from '@/features/invites/pages/AcceptInvitePage';
import { SessionManagementPage } from '@/features/sessions/pages/SessionManagementPage';
import { SearchPage } from '@/features/search/pages/SearchPage';
import { ReportsPage } from '@/features/reports/pages/ReportsPage';
import { PlatformAdminOrganizationsPage } from '@/features/admin/pages/PlatformAdminOrganizationsPage';
import { PlatformAdminBillingPage } from '@/features/admin/pages/PlatformAdminBillingPage';
import { PlatformAdminOrgBillingDetailPage } from '@/features/admin/pages/PlatformAdminOrgBillingDetailPage';
import { PlatformAdminPlansPage } from '@/features/admin/pages/PlatformAdminPlansPage';
import { NotificationHistoryPage } from '@/features/notifications/pages/NotificationHistoryPage';
import { AuditLogsPage } from '@/features/admin/pages/AuditLogsPage';
import { ErrorLogsPage } from '@/features/admin/pages/ErrorLogsPage';
import { ReferenceDataPage } from '@/features/admin/pages/ReferenceDataPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { BillingPage } from '@/features/billing/pages/BillingPage';
import { PlanComparisonPage } from '@/features/billing/pages/PlanComparisonPage';
import { AnalyticsDashboardPage } from '@/features/analytics/pages/AnalyticsDashboardPage';
import { TimeTrackingPage } from '@/features/time-tracking/pages/TimeTrackingPage';
import { TimeTrackingSettingsPage } from '@/features/time-tracking/pages/TimeTrackingSettingsPage';

export const router = createBrowserRouter([
    // --- Guest routes (redirect away if already authenticated) ---
    {
        element: <GuestGuard />,
        children: [
            {
                element: <AuthLayout />,
                children: [
                    { path: '/login', element: <LoginPage /> },
                    { path: '/admin/login', element: <PlatformAdminLoginPage /> },
                    { path: '/password/reset', element: <PasswordResetPage /> },
                ],
            },
        ],
    },

    // --- Public route (no auth required) ---
    {
        element: <AuthLayout />,
        children: [
            { path: '/invites/:token', element: <AcceptInvitePage /> },
        ],
    },

    // --- Forced password change (auth + first-time only) ---
    {
        element: <AuthGuard />,
        children: [
            {
                element: <FirstTimeGuard />,
                children: [
                    {
                        element: <AuthLayout />,
                        children: [
                            { path: '/password/change', element: <ForcedPasswordChangePage /> },
                        ],
                    },
                ],
            },
        ],
    },

    // --- Authenticated app routes (wrapped in AppShell) ---
    {
        element: <AuthGuard />,
        children: [
            {
                element: <OrgUserGuard />,
                children: [
                    {
                        element: <AppShell />,
                        children: [
                            // General app routes
                            { path: '/', element: <DashboardPage /> },
                            { path: '/projects', element: <ProjectListPage /> },
                            { path: '/projects/:id', element: <ProjectDetailPage /> },
                            { path: '/stories', element: <StoryListPage /> },
                            { path: '/stories/:id', element: <StoryDetailPage /> },
                            { path: '/stories/key/:key', element: <StoryByKeyRedirect /> },
                            { path: '/boards/kanban', element: <KanbanBoardPage /> },
                            { path: '/boards/sprint', element: <SprintBoardPage /> },
                            { path: '/boards/department', element: <DepartmentBoardPage /> },
                            { path: '/boards/backlog', element: <BacklogPage /> },
                            { path: '/sprints', element: <SprintListPage /> },
                            { path: '/sprints/:id', element: <SprintDetailPage /> },
                            { path: '/members', element: <MemberListPage /> },
                            { path: '/members/:id', element: <MemberProfilePage /> },
                            { path: '/departments', element: <DepartmentListPage /> },
                            { path: '/departments/:id', element: <DepartmentDetailPage /> },
                            { path: '/preferences', element: <PreferencesPage /> },
                            { path: '/sessions', element: <SessionManagementPage /> },
                            { path: '/search', element: <SearchPage /> },
                            { path: '/reports', element: <ReportsPage /> },
                            { path: '/analytics', element: <AnalyticsDashboardPage /> },
                            { path: '/time-tracking', element: <TimeTrackingPage /> },
                            { path: '/notifications', element: <NotificationHistoryPage /> },

                            // OrgAdmin-only routes
                            {
                                element: <RoleGuard allowedRoles={['OrgAdmin']} />,
                                children: [
                                    { path: '/settings', element: <SettingsPage /> },
                                    { path: '/billing', element: <BillingPage /> },
                                    { path: '/billing/plans', element: <PlanComparisonPage /> },
                                    { path: '/time-tracking/settings', element: <TimeTrackingSettingsPage /> },
                                    { path: '/audit-logs', element: <AuditLogsPage /> },
                                    { path: '/settings/error-logs', element: <ErrorLogsPage /> },
                                    { path: '/settings/reference-data', element: <ReferenceDataPage /> },
                                ],
                            },

                            // OrgAdmin + DeptLead routes
                            {
                                element: <RoleGuard allowedRoles={['OrgAdmin', 'DeptLead']} />,
                                children: [
                                    { path: '/invites', element: <InviteManagementPage /> },
                                ],
                            },
                        ],
                    },
                ],
            },

            // PlatformAdmin routes (wrapped in AdminLayout)
            {
                element: <RoleGuard allowedRoles={['PlatformAdmin']} />,
                children: [
                    {
                        element: <AdminLayout />,
                        children: [
                            { path: '/admin/organizations', element: <PlatformAdminOrganizationsPage /> },
                            { path: '/admin/billing', element: <PlatformAdminBillingPage /> },
                            { path: '/admin/billing/organizations/:id', element: <PlatformAdminOrgBillingDetailPage /> },
                            { path: '/admin/billing/plans', element: <PlatformAdminPlansPage /> },
                            { path: '/admin/audit-logs', element: <AuditLogsPage /> },
                        ],
                    },
                ],
            },
        ],
    },

    // --- Catch-all ---
    { path: '*', element: <NotFoundPage /> },
]);
