import { useEffect, useRef } from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { useToast } from '@/components/common/Toast';

interface RoleGuardProps {
    allowedRoles: string[];
}

export function RoleGuard({ allowedRoles }: RoleGuardProps) {
    const { user } = useAuth();
    const { addToast } = useToast();
    const hasAccess = user && allowedRoles.includes(user.roleName);
    const toastShown = useRef(false);

    useEffect(() => {
        if (user && !hasAccess && !toastShown.current) {
            toastShown.current = true;
            addToast('error', 'You don\'t have permission to access this page.');
        }
    }, [user, hasAccess, addToast]);

    if (!hasAccess) {
        return <Navigate to="/" replace />;
    }

    return <Outlet />;
}
