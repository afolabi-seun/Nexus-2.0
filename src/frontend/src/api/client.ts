import axios, {
    type AxiosInstance,
    type AxiosError,
    type InternalAxiosRequestConfig,
} from 'axios';
import { ApiError } from '@/types/api';
import type { ApiResponse } from '@/types/api';
import type { LoginResponse } from '@/types/auth';

export interface CreateApiClientOptions {
    baseURL: string;
    withCredentials?: boolean;
}

let isRefreshing = false;
let failedQueue: {
    resolve: (token: string) => void;
    reject: (err: unknown) => void;
}[] = [];

function processQueue(error: unknown, token: string | null) {
    failedQueue.forEach((p) => {
        if (error) {
            p.reject(error);
        } else {
            p.resolve(token!);
        }
    });
    failedQueue = [];
}

function generateCorrelationId(): string {
    return crypto.randomUUID();
}

// Lazy accessor to break circular dependency with authStore
function getAuthStore() {
    // eslint-disable-next-line @typescript-eslint/no-var-requires
    return import('@/stores/authStore').then((m) => m.useAuthStore);
}

let _authStoreRef: typeof import('@/stores/authStore').useAuthStore | null = null;

// Synchronous accessor — populated after first async load
function authStore() {
    return _authStoreRef!;
}

// Pre-load the auth store reference
getAuthStore().then((store) => {
    _authStoreRef = store;
});

export function createApiClient(options: CreateApiClientOptions): AxiosInstance {
    const instance = axios.create({
        baseURL: options.baseURL,
        headers: { 'Content-Type': 'application/json' },
        withCredentials: options.withCredentials ?? false,
    });

    // Request interceptor: attach JWT + correlation ID
    instance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
        if (_authStoreRef) {
            const accessToken = authStore().getState().accessToken;
            if (accessToken) {
                config.headers.Authorization = `Bearer ${accessToken}`;
            }
        }
        config.headers['X-Correlation-Id'] = generateCorrelationId();
        return config;
    });

    // Response interceptor: unwrap ApiResponse or throw ApiError
    instance.interceptors.response.use(
        (response) => {
            const body = response.data as ApiResponse<unknown>;
            if (body && typeof body === 'object' && 'responseCode' in body) {
                if (body.errorCode) {
                    throw new ApiError(
                        body.message ?? 'An error occurred',
                        body.errorCode,
                        body.errorValue ?? 0,
                        body.errors,
                        body.correlationId
                    );
                }
                response.data = body.data;
            }
            return response;
        },
        async (error: AxiosError<ApiResponse<unknown>>) => {
            const originalRequest = error.config as InternalAxiosRequestConfig & {
                _retry?: boolean;
            };

            if (error.response?.status === 401 && !originalRequest._retry) {
                const responseBody = error.response.data;

                // Detect REFRESH_TOKEN_REUSE
                if (responseBody?.errorCode === 'REFRESH_TOKEN_REUSE') {
                    if (_authStoreRef) authStore().getState().logout();
                    window.location.href = '/login';
                    return Promise.reject(
                        new ApiError(
                            'Session expired. Please log in again.',
                            'REFRESH_TOKEN_REUSE',
                            0
                        )
                    );
                }

                if (isRefreshing) {
                    return new Promise((resolve, reject) => {
                        failedQueue.push({
                            resolve: (token: string) => {
                                originalRequest.headers.Authorization = `Bearer ${token}`;
                                resolve(instance(originalRequest));
                            },
                            reject,
                        });
                    });
                }

                originalRequest._retry = true;
                isRefreshing = true;

                try {
                    if (!_authStoreRef) {
                        _authStoreRef = await getAuthStore();
                    }

                    const { env } = await import('@/utils/env');
                    const refreshResponse = await axios.post<ApiResponse<LoginResponse>>(
                        `${env.SECURITY_API_URL}/api/v1/auth/refresh`,
                        { deviceId: 'web' },
                        { withCredentials: true }
                    );

                    const apiResp = refreshResponse.data;
                    if (apiResp.errorCode) {
                        throw new ApiError(
                            apiResp.message ?? 'Refresh failed',
                            apiResp.errorCode,
                            apiResp.errorValue ?? 0
                        );
                    }

                    const tokens = apiResp.data!;
                    authStore()
                        .getState()
                        .refreshTokens(tokens.accessToken);

                    processQueue(null, tokens.accessToken);

                    originalRequest.headers.Authorization = `Bearer ${tokens.accessToken}`;
                    return instance(originalRequest);
                } catch (refreshError) {
                    processQueue(refreshError, null);
                    if (_authStoreRef) authStore().getState().logout();
                    window.location.href = '/login';
                    return Promise.reject(refreshError);
                } finally {
                    isRefreshing = false;
                }
            }

            // Non-401 errors: parse ApiError from response body
            if (error.response?.data) {
                const body = error.response.data;
                if (body.errorCode) {
                    return Promise.reject(
                        new ApiError(
                            body.message ?? 'An error occurred',
                            body.errorCode,
                            body.errorValue ?? 0,
                            body.errors,
                            body.correlationId
                        )
                    );
                }
            }

            // Network / unknown errors
            return Promise.reject(
                new ApiError(
                    'Unable to connect to the server. Please check your connection.',
                    'NETWORK_ERROR',
                    0
                )
            );
        }
    );

    return instance;
}
