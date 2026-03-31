import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';

/** Redirects PlatformAdmin users to the admin panel. Allows org-scoped users through. */
export function OrgUserGuard() {
    const { user } = useAuth();

    if (user?.roleName === 'PlatformAdmin') {
        return <Navigate to="/admin/organizations" replace />;
    }

    return <Outlet />;
}
