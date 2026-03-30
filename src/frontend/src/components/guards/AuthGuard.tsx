import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';

const REDIRECT_KEY = 'nexus_redirect';

export function AuthGuard() {
    const { isAuthenticated, isFirstTimeUser } = useAuth();
    const location = useLocation();

    if (!isAuthenticated) {
        sessionStorage.setItem(REDIRECT_KEY, location.pathname + location.search);
        return <Navigate to="/login" replace />;
    }

    if (isFirstTimeUser && location.pathname !== '/password/change') {
        return <Navigate to="/password/change" replace />;
    }

    return <Outlet />;
}
