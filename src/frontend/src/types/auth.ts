export interface LoginRequest {
    email: string;
    password: string;
    deviceId?: string;
}

export interface LoginResponse {
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
    isFirstTimeUser: boolean;
}

export interface RefreshTokenRequest {
    refreshToken: string;
}

export interface ForcedPasswordChangeRequest {
    newPassword: string;
    confirmPassword: string;
}

export interface PasswordResetRequest {
    email: string;
}

export interface PasswordResetConfirmRequest {
    email: string;
    otp: string;
    newPassword: string;
    confirmPassword: string;
}

export interface SessionResponse {
    sessionId: string;
    deviceId: string;
    deviceInfo: string | null;
    ipAddress: string | null;
    createdAt: string;
}

export interface AuthUser {
    userId: string;
    organizationId: string | null;
    departmentId: string | null;
    roleName: string;
    email: string;
    firstName: string;
    lastName: string;
    isFirstTimeUser: boolean;
}
