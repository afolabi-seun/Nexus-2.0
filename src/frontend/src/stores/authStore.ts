import { create } from 'zustand';
import type { AuthUser } from '@/types/auth';

interface AuthState {
    accessToken: string | null;
    refreshToken: string | null;
    user: AuthUser | null;
    isAuthenticated: boolean;
    isPlatformAdmin: boolean;
    isFirstTimeUser: boolean;
}

interface AuthActions {
    login(
        tokens: { accessToken: string; refreshToken?: string },
        user: AuthUser,
        isFirstTimeUser?: boolean
    ): void;
    logout(): void;
    refreshTokens(accessToken: string, refreshToken?: string): void;
    setUser(user: AuthUser): void;
}

function decodeJwtPayload(token: string): Record<string, unknown> {
    try {
        const base64 = token.split('.')[1];
        const json = atob(base64.replace(/-/g, '+').replace(/_/g, '/'));
        return JSON.parse(json);
    } catch {
        return {};
    }
}

export function extractUserFromToken(token: string): AuthUser {
    const payload = decodeJwtPayload(token);
    return {
        userId: (payload.sub ?? payload.userId ?? '') as string,
        organizationId: (payload.organizationId ?? null) as string | null,
        departmentId: (payload.departmentId ?? null) as string | null,
        roleName: (payload.roleName ?? payload.role ?? '') as string,
        email: (payload.email ?? '') as string,
        firstName: (payload.firstName ?? '') as string,
        lastName: (payload.lastName ?? '') as string,
        isFirstTimeUser: payload.isFirstTimeUser === true || payload.isFirstTimeUser === 'true',
    };
}

export const useAuthStore = create<AuthState & AuthActions>()((set) => ({
    accessToken: null,
    refreshToken: null,
    user: null,
    isAuthenticated: false,
    isPlatformAdmin: false,
    isFirstTimeUser: false,

    login(tokens, user, isFirstTimeUser) {
        const isPlatformAdmin =
            !user.organizationId && user.roleName === 'PlatformAdmin';
        const firstTime = isFirstTimeUser ?? user.isFirstTimeUser;
        set({
            accessToken: tokens.accessToken,
            refreshToken: null, // refresh token is now in httpOnly cookie
            user: { ...user, isFirstTimeUser: firstTime },
            isAuthenticated: true,
            isPlatformAdmin,
            isFirstTimeUser: firstTime,
        });
    },

    logout() {
        set({
            accessToken: null,
            refreshToken: null,
            user: null,
            isAuthenticated: false,
            isPlatformAdmin: false,
            isFirstTimeUser: false,
        });
    },

    refreshTokens(accessToken) {
        set({ accessToken });
    },

    setUser(user) {
        const isPlatformAdmin =
            !user.organizationId && user.roleName === 'PlatformAdmin';
        set({
            user,
            isPlatformAdmin,
            isFirstTimeUser: user.isFirstTimeUser,
        });
    },
}));
