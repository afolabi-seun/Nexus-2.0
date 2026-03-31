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
        tokens: { accessToken: string; refreshToken: string },
        user: AuthUser,
        isFirstTimeUser?: boolean
    ): void;
    logout(): void;
    refreshTokens(accessToken: string, refreshToken: string): void;
    setUser(user: AuthUser): void;
}

const REFRESH_TOKEN_KEY = 'nexus_rt';

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

function loadPersistedRefreshToken(): string | null {
    try {
        return localStorage.getItem(REFRESH_TOKEN_KEY);
    } catch {
        return null;
    }
}

function persistRefreshToken(token: string | null): void {
    try {
        if (token) {
            localStorage.setItem(REFRESH_TOKEN_KEY, token);
        } else {
            localStorage.removeItem(REFRESH_TOKEN_KEY);
        }
    } catch {
        // localStorage unavailable
    }
}

export const useAuthStore = create<AuthState & AuthActions>()((set) => ({
    accessToken: null,
    refreshToken: loadPersistedRefreshToken(),
    user: null,
    isAuthenticated: false,
    isPlatformAdmin: false,
    isFirstTimeUser: false,

    login(tokens, user, isFirstTimeUser) {
        persistRefreshToken(tokens.refreshToken);
        const isPlatformAdmin =
            !user.organizationId && user.roleName === 'PlatformAdmin';
        const firstTime = isFirstTimeUser ?? user.isFirstTimeUser;
        set({
            accessToken: tokens.accessToken,
            refreshToken: tokens.refreshToken,
            user: { ...user, isFirstTimeUser: firstTime },
            isAuthenticated: true,
            isPlatformAdmin,
            isFirstTimeUser: firstTime,
        });
    },

    logout() {
        persistRefreshToken(null);
        set({
            accessToken: null,
            refreshToken: null,
            user: null,
            isAuthenticated: false,
            isPlatformAdmin: false,
            isFirstTimeUser: false,
        });
    },

    refreshTokens(accessToken, refreshToken) {
        persistRefreshToken(refreshToken);
        set({ accessToken, refreshToken });
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
