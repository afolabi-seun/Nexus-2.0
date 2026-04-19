import { createApiClient } from './client';
import { env } from '@/utils/env';
import type {
    LoginRequest,
    LoginResponse,
    RefreshTokenRequest,
    ForcedPasswordChangeRequest,
    PasswordResetRequest,
    PasswordResetConfirmRequest,
    SessionResponse,
} from '@/types/auth';

const client = createApiClient({ baseURL: env.SECURITY_API_URL, withCredentials: true });

export const securityApi = {
    login: (data: LoginRequest): Promise<LoginResponse> =>
        client.post('/api/v1/auth/login', data).then((r) => r.data),

    refreshToken: (data: RefreshTokenRequest): Promise<LoginResponse> =>
        client.post('/api/v1/auth/refresh', data).then((r) => r.data),

    logout: (): Promise<void> =>
        client.post('/api/v1/auth/logout').then(() => undefined),

    forcedPasswordChange: (data: ForcedPasswordChangeRequest): Promise<void> =>
        client.post('/api/v1/auth/password/change', data).then(() => undefined),

    requestPasswordReset: (data: PasswordResetRequest): Promise<void> =>
        client.post('/api/v1/auth/password/reset', data).then(() => undefined),

    confirmPasswordReset: (data: PasswordResetConfirmRequest): Promise<void> =>
        client.post('/api/v1/auth/password/reset/confirm', data).then(() => undefined),

    getSessions: (): Promise<SessionResponse[]> =>
        client.get('/api/v1/auth/sessions').then((r) => r.data),

    revokeSession: (sessionId: string): Promise<void> =>
        client.delete(`/api/v1/auth/sessions/${sessionId}`).then(() => undefined),

    revokeAllSessions: (): Promise<void> =>
        client.delete('/api/v1/auth/sessions').then(() => undefined),

    // OTP
    requestOtp: (data: { email: string }): Promise<void> =>
        client.post('/api/v1/auth/otp/request', data).then(() => undefined),

    verifyOtp: (data: { email: string; otp: string }): Promise<{ verified: boolean }> =>
        client.post('/api/v1/auth/otp/verify', data).then((r) => r.data),
};
