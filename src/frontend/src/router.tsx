import { lazy, Suspense } from 'react';
import { createBrowserRouter } from 'react-router-dom';

// Guards (always loaded — used on every route)
import { AuthGuard } from '@/components/guards/AuthGuard';
import { GuestGuard } from '@/components/guards/GuestGuard';
import { RoleGuard } from '@/components/guards/RoleGuard';
import { OrgUserGuard } from '@/components/guards/OrgUserGuard';
import { FirstTimeGuard } from '@/components/guards/FirstTimeGuard';

// Layouts (always loaded — structural)
import { AppShell } from '@/components/layout/AppShell';
import { AuthLayout } from '@/components/layout/AuthLayout';
import { AdminLayout } from '@/components/layout/AdminLayout';

// Auth pages (always loaded — entry point)
import { LoginPage } from '@/features/auth/pages/LoginPage';
import { PlatformAdminLoginPage } from '@/features/auth/pages/PlatformAdminLoginPage';
import { ForcedPasswordChangePage } from '@/features/auth/pages/ForcedPasswordChangePage';
import { PasswordResetPage } from '@/features/auth/pages/PasswordResetPage';

// Core pages (static — high-traffic, loaded immediately)
import { DashboardPage } from '@/features/dashboard/pages/DashboardPage';
import { ProjectListPage } from '@/features/projects/pages/ProjectListPage';
import { ProjectDetailPage } from '@/features/projects/pages/ProjectDetailPage';
import { StoryListPage } from '@/features/stories/pages/StoryListPage';
import { StoryDetailPage } from '@/features/stories/pages/StoryDetailPage';
import { StoryByKeyRedirect } from '@/features/stories/pages/StoryByKeyRedirect';
import { KanbanBoardPage } from '@/features/boards/pages/KanbanBoardPage';
import { SprintBoardPage } from '@/features/boards/pages/SprintBoardPage';
import { SprintListPage } from '@/features/sprints/pages/SprintListPage';
import { SprintDetailPage } from '@/features/sprints/pages/SprintDetailPage';
import { NotFoundPage } from '@/pages/NotFoundPage';

// Lazy-loaded pages (less frequent access — split into separate chunks)
const DepartmentBoardPage = lazy(() => import('@/features/boards/pages/DepartmentBoardPage').then(m => ({ default: m.DepartmentBoardPage })));
const BacklogPage = lazy(() => import('@/features/boards/pages/BacklogPage').then(m => ({ default: m.BacklogPage })));
const MemberListPage = lazy(() => import('@/features/members/pages/MemberListPage').then(m => ({ default: m.MemberListPage })));
const MemberProfilePage = lazy(() => import('@/features/members/pages/MemberProfilePage').then(m => ({ default: m.MemberProfilePage })));
const DepartmentListPage = lazy(() => import('@/features/departments/pages/DepartmentListPage').then(m => ({ default: m.DepartmentListPage })));
const DepartmentDetailPage = lazy(() => import('@/features/departments/pages/DepartmentDetailPage').then(m => ({ default: m.DepartmentDetailPage })));
const SettingsPage = lazy(() => import('@/features/settings/pages/SettingsPage').then(m => ({ default: m.SettingsPage })));
const PreferencesPage = lazy(() => import('@/features/preferences/pages/PreferencesPage').then(m => ({ default: m.PreferencesPage })));
const InviteManagementPage = lazy(() => import('@/features/invites/pages/InviteManagementPage').then(m => ({ default: m.InviteManagementPage })));
const AcceptInvitePage = lazy(() => import('@/features/invites/pages/AcceptInvitePage').then(m => ({ default: m.AcceptInvitePage })));
const SessionManagementPage = lazy(() => import('@/features/sessions/pages/SessionManagementPage').then(m => ({ default: m.SessionManagementPage })));
const SearchPage = lazy(() => import('@/features/search/pages/SearchPage').then(m => ({ default: m.SearchPage })));
const ReportsPage = lazy(() => import('@/features/reports/pages/ReportsPage').then(m => ({ default: m.ReportsPage })));
const NotificationHistoryPage = lazy(() => import('@/features/notifications/pages/NotificationHistoryPage').then(m => ({ default: m.NotificationHistoryPage })));
const BillingPage = lazy(() => import('@/features/billing/pages/BillingPage').then(m => ({ default: m.BillingPage })));
const PlanComparisonPage = lazy(() => import('@/features/billing/pages/PlanComparisonPage').then(m => ({ default: m.PlanComparisonPage })));
const AnalyticsDashboardPage = lazy(() => import('@/features/analytics/pages/AnalyticsDashboardPage').then(m => ({ default: m.AnalyticsDashboardPage })));
const TimeTrackingPage = lazy(() => import('@/features/time-tracking/pages/TimeTrackingPage').then(m => ({ default: m.TimeTrackingPage })));
const TimeTrackingSettingsPage = lazy(() => import('@/features/time-tracking/pages/TimeTrackingSettingsPage').then(m => ({ default: m.TimeTrackingSettingsPage })));
const AuditLogsPage = lazy(() => import('@/features/admin/pages/AuditLogsPage').then(m => ({ default: m.AuditLogsPage })));
const ErrorLogsPage = lazy(() => import('@/features/admin/pages/ErrorLogsPage').then(m => ({ default: m.ErrorLogsPage })));
const ReferenceDataPage = lazy(() => import('@/features/admin/pages/ReferenceDataPage').then(m => ({ default: m.ReferenceDataPage })));
const PlatformAdminOrganizationsPage = lazy(() => import('@/features/admin/pages/PlatformAdminOrganizationsPage').then(m => ({ default: m.PlatformAdminOrganizationsPage })));
const PlatformAdminBillingPage = lazy(() => import('@/features/admin/pages/PlatformAdminBillingPage').then(m => ({ default: m.PlatformAdminBillingPage })));
const PlatformAdminOrgBillingDetailPage = lazy(() => import('@/features/admin/pages/PlatformAdminOrgBillingDetailPage').then(m => ({ default: m.PlatformAdminOrgBillingDetailPage })));
const PlatformAdminPlansPage = lazy(() => import('@/features/admin/pages/PlatformAdminPlansPage').then(m => ({ default: m.PlatformAdminPlansPage })));

function LazyFallback() {
    return (
        <div className="flex h-32 items-center justify-center">
            <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
        </div>
    );
}

function L({ children }: { children: React.ReactNode }) {
    return <Suspense fallback={<LazyFallback />}>{children}</Suspense>;
}

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
            { path: '/invites/:token', element: <L><AcceptInvitePage /></L> },
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
                            // Core routes (static imports — instant navigation)
                            { path: '/', element: <DashboardPage /> },
                            { path: '/projects', element: <ProjectListPage /> },
                            { path: '/projects/:id', element: <ProjectDetailPage /> },
                            { path: '/stories', element: <StoryListPage /> },
                            { path: '/stories/:id', element: <StoryDetailPage /> },
                            { path: '/stories/key/:key', element: <StoryByKeyRedirect /> },
                            { path: '/boards/kanban', element: <KanbanBoardPage /> },
                            { path: '/boards/sprint', element: <SprintBoardPage /> },
                            { path: '/sprints', element: <SprintListPage /> },
                            { path: '/sprints/:id', element: <SprintDetailPage /> },

                            // Lazy-loaded routes
                            { path: '/boards/department', element: <L><DepartmentBoardPage /></L> },
                            { path: '/boards/backlog', element: <L><BacklogPage /></L> },
                            { path: '/members', element: <L><MemberListPage /></L> },
                            { path: '/members/:id', element: <L><MemberProfilePage /></L> },
                            { path: '/departments', element: <L><DepartmentListPage /></L> },
                            { path: '/departments/:id', element: <L><DepartmentDetailPage /></L> },
                            { path: '/preferences', element: <L><PreferencesPage /></L> },
                            { path: '/sessions', element: <L><SessionManagementPage /></L> },
                            { path: '/search', element: <L><SearchPage /></L> },
                            { path: '/reports', element: <L><ReportsPage /></L> },
                            { path: '/analytics', element: <L><AnalyticsDashboardPage /></L> },
                            { path: '/time-tracking', element: <L><TimeTrackingPage /></L> },
                            { path: '/notifications', element: <L><NotificationHistoryPage /></L> },

                            // OrgAdmin-only routes (lazy)
                            {
                                element: <RoleGuard allowedRoles={['OrgAdmin']} />,
                                children: [
                                    { path: '/settings', element: <L><SettingsPage /></L> },
                                    { path: '/billing', element: <L><BillingPage /></L> },
                                    { path: '/billing/plans', element: <L><PlanComparisonPage /></L> },
                                    { path: '/time-tracking/settings', element: <L><TimeTrackingSettingsPage /></L> },
                                    { path: '/audit-logs', element: <L><AuditLogsPage /></L> },
                                    { path: '/settings/error-logs', element: <L><ErrorLogsPage /></L> },
                                    { path: '/settings/reference-data', element: <L><ReferenceDataPage /></L> },
                                ],
                            },

                            // OrgAdmin + DeptLead routes (lazy)
                            {
                                element: <RoleGuard allowedRoles={['OrgAdmin', 'DeptLead']} />,
                                children: [
                                    { path: '/invites', element: <L><InviteManagementPage /></L> },
                                ],
                            },
                        ],
                    },
                ],
            },

            // PlatformAdmin routes (lazy, wrapped in AdminLayout)
            {
                element: <RoleGuard allowedRoles={['PlatformAdmin']} />,
                children: [
                    {
                        element: <AdminLayout />,
                        children: [
                            { path: '/admin/organizations', element: <L><PlatformAdminOrganizationsPage /></L> },
                            { path: '/admin/billing', element: <L><PlatformAdminBillingPage /></L> },
                            { path: '/admin/billing/organizations/:id', element: <L><PlatformAdminOrgBillingDetailPage /></L> },
                            { path: '/admin/billing/plans', element: <L><PlatformAdminPlansPage /></L> },
                            { path: '/admin/audit-logs', element: <L><AuditLogsPage /></L> },
                        ],
                    },
                ],
            },
        ],
    },

    // --- Catch-all ---
    { path: '*', element: <NotFoundPage /> },
]);
