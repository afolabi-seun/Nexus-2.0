import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';

export function FirstTimeGuard() {
    const { isFirstTimeUser } = useAuth();

    if (!isFirstTimeUser) {
        return <Navigate to="/" replace />;
    }

    return <Outlet />;
}
