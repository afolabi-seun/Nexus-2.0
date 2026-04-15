import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { AuthGuard } from '@/components/guards/AuthGuard';
import { GuestGuard } from '@/components/guards/GuestGuard';
import { RoleGuard } from '@/components/guards/RoleGuard';
import { FirstTimeGuard } from '@/components/guards/FirstTimeGuard';
import { ToastProvider } from '@/components/common/Toast';
import type { AuthUser } from '@/types/auth';

/**
 * **Validates: Properties 9, 10, 11; Requirements 8.2, 8.3, 8.4, 4.7**
 *
 * Unit tests for route guards:
 * - AuthGuard redirects unauthenticated users to /login and stores intended destination
 * - RoleGuard redirects unauthorized users to /
 * - GuestGuard redirects authenticated users to /
 * - FirstTimeGuard redirects non-first-time users to /
 */

const testUser: AuthUser = {
    userId: 'user-1',
    organizationId: 'org-1',
    departmentId: 'dept-1',
    roleName: 'Member',
    email: 'test@example.com',
    firstName: 'Test',
    lastName: 'User',
    isFirstTimeUser: false,
};

function renderWithRouter(ui: React.ReactElement, { route = '/' } = {}) {
    return render(
        <ToastProvider>
            <MemoryRouter initialEntries={[route]}>
                {ui}
            </MemoryRouter>
        </ToastProvider>
    );
}

describe('AuthGuard', () => {
    beforeEach(() => {
        useAuthStore.getState().logout();
        sessionStorage.clear();
    });

    it('redirects unauthenticated users to /login', () => {
        renderWithRouter(
            <Routes>
                <Route element={<AuthGuard />}>
                    <Route path="/dashboard" element={<div>Dashboard</div>} />
                </Route>
                <Route path="/login" element={<div>Login Page</div>} />
            </Routes>,
            { route: '/dashboard' }
        );

        expect(screen.getByText('Login Page')).toBeInTheDocument();
    });

    it('stores intended destination in sessionStorage', () => {
        renderWithRouter(
            <Routes>
                <Route element={<AuthGuard />}>
                    <Route path="/dashboard" element={<div>Dashboard</div>} />
                </Route>
                <Route path="/login" element={<div>Login Page</div>} />
            </Routes>,
            { route: '/dashboard' }
        );

        expect(sessionStorage.getItem('nexus_redirect')).toBe('/dashboard');
    });

    it('allows authenticated users through', () => {
        useAuthStore.getState().login(
            { accessToken: 'at', refreshToken: 'rt' },
            testUser
        );

        renderWithRouter(
            <Routes>
                <Route element={<AuthGuard />}>
                    <Route path="/dashboard" element={<div>Dashboard</div>} />
                </Route>
                <Route path="/login" element={<div>Login Page</div>} />
            </Routes>,
            { route: '/dashboard' }
        );

        expect(screen.getByText('Dashboard')).toBeInTheDocument();
    });

    it('redirects first-time users to /password/change', () => {
        const firstTimeUser = { ...testUser, isFirstTimeUser: true };
        useAuthStore.getState().login(
            { accessToken: 'at', refreshToken: 'rt' },
            firstTimeUser
        );

        renderWithRouter(
            <Routes>
                <Route element={<AuthGuard />}>
                    <Route path="/dashboard" element={<div>Dashboard</div>} />
                    <Route path="/password/change" element={<div>Change Password</div>} />
                </Route>
                <Route path="/login" element={<div>Login Page</div>} />
            </Routes>,
            { route: '/dashboard' }
        );

        expect(screen.getByText('Change Password')).toBeInTheDocument();
    });
});

describe('GuestGuard', () => {
    beforeEach(() => {
        useAuthStore.getState().logout();
    });

    it('allows unauthenticated users through', () => {
        renderWithRouter(
            <Routes>
                <Route element={<GuestGuard />}>
                    <Route path="/login" element={<div>Login Page</div>} />
                </Route>
                <Route path="/" element={<div>Home</div>} />
            </Routes>,
            { route: '/login' }
        );

        expect(screen.getByText('Login Page')).toBeInTheDocument();
    });

    it('redirects authenticated users to /', () => {
        useAuthStore.getState().login(
            { accessToken: 'at', refreshToken: 'rt' },
            testUser
        );

        renderWithRouter(
            <Routes>
                <Route element={<GuestGuard />}>
                    <Route path="/login" element={<div>Login Page</div>} />
                </Route>
                <Route path="/" element={<div>Home</div>} />
            </Routes>,
            { route: '/login' }
        );

        expect(screen.getByText('Home')).toBeInTheDocument();
    });
});

describe('RoleGuard', () => {
    beforeEach(() => {
        useAuthStore.getState().logout();
    });

    it('allows users with matching role through', () => {
        const adminUser = { ...testUser, roleName: 'OrgAdmin' };
        useAuthStore.getState().login(
            { accessToken: 'at', refreshToken: 'rt' },
            adminUser
        );

        renderWithRouter(
            <Routes>
                <Route element={<RoleGuard allowedRoles={['OrgAdmin']} />}>
                    <Route path="/settings" element={<div>Settings</div>} />
                </Route>
                <Route path="/" element={<div>Home</div>} />
            </Routes>,
            { route: '/settings' }
        );

        expect(screen.getByText('Settings')).toBeInTheDocument();
    });

    it('redirects users without matching role to /', () => {
        useAuthStore.getState().login(
            { accessToken: 'at', refreshToken: 'rt' },
            testUser // roleName: 'Member'
        );

        renderWithRouter(
            <Routes>
                <Route element={<RoleGuard allowedRoles={['OrgAdmin']} />}>
                    <Route path="/settings" element={<div>Settings</div>} />
                </Route>
                <Route path="/" element={<div>Home</div>} />
            </Routes>,
            { route: '/settings' }
        );

        expect(screen.getByText('Home')).toBeInTheDocument();
    });

    it('allows users with any of the allowed roles', () => {
        const deptLead = { ...testUser, roleName: 'DeptLead' };
        useAuthStore.getState().login(
            { accessToken: 'at', refreshToken: 'rt' },
            deptLead
        );

        renderWithRouter(
            <Routes>
                <Route element={<RoleGuard allowedRoles={['OrgAdmin', 'DeptLead']} />}>
                    <Route path="/invites" element={<div>Invites</div>} />
                </Route>
                <Route path="/" element={<div>Home</div>} />
            </Routes>,
            { route: '/invites' }
        );

        expect(screen.getByText('Invites')).toBeInTheDocument();
    });
});

describe('FirstTimeGuard', () => {
    beforeEach(() => {
        useAuthStore.getState().logout();
    });

    it('allows first-time users through', () => {
        const firstTimeUser = { ...testUser, isFirstTimeUser: true };
        useAuthStore.getState().login(
            { accessToken: 'at', refreshToken: 'rt' },
            firstTimeUser
        );

        renderWithRouter(
            <Routes>
                <Route element={<FirstTimeGuard />}>
                    <Route path="/password/change" element={<div>Change Password</div>} />
                </Route>
                <Route path="/" element={<div>Home</div>} />
            </Routes>,
            { route: '/password/change' }
        );

        expect(screen.getByText('Change Password')).toBeInTheDocument();
    });

    it('redirects non-first-time users to /', () => {
        useAuthStore.getState().login(
            { accessToken: 'at', refreshToken: 'rt' },
            testUser // isFirstTimeUser: false
        );

        renderWithRouter(
            <Routes>
                <Route element={<FirstTimeGuard />}>
                    <Route path="/password/change" element={<div>Change Password</div>} />
                </Route>
                <Route path="/" element={<div>Home</div>} />
            </Routes>,
            { route: '/password/change' }
        );

        expect(screen.getByText('Home')).toBeInTheDocument();
    });
});
