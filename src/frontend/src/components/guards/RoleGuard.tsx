import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';

interface RoleGuardProps {
    allowedRoles: string[];
}

export function RoleGuard({ allowedRoles }: RoleGuardProps) {
    const { user } = useAuth();

    if (!user || !allowedRoles.includes(user.roleName)) {
        // TODO: show permission-denied toast once Toast component is available
        return <Navigate to="/" replace />;
    }

    return <Outlet />;
}
