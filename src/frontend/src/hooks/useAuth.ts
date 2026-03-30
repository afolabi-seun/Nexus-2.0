import { useAuthStore } from '@/stores/authStore';

export function useAuth() {
    const user = useAuthStore((s) => s.user);
    const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
    const isPlatformAdmin = useAuthStore((s) => s.isPlatformAdmin);
    const isFirstTimeUser = useAuthStore((s) => s.isFirstTimeUser);
    const login = useAuthStore((s) => s.login);
    const logout = useAuthStore((s) => s.logout);

    return {
        user,
        isAuthenticated,
        isPlatformAdmin,
        isFirstTimeUser,
        login,
        logout,
    };
}
