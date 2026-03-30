export interface ApiResponse<T> {
    responseCode: string;
    responseDescription: string;
    data: T | null;
    errorCode: string | null;
    errorValue: number | null;
    message: string | null;
    correlationId: string | null;
    errors: ErrorDetail[] | null;
}

export interface PaginatedResponse<T> {
    data: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
}

export interface ErrorDetail {
    field: string;
    message: string;
}

export class ApiError extends Error {
    errorCode: string;
    errorValue: number;
    errors: ErrorDetail[] | null;
    correlationId: string | null;

    constructor(
        message: string,
        errorCode: string,
        errorValue: number,
        errors: ErrorDetail[] | null = null,
        correlationId: string | null = null
    ) {
        super(message);
        this.name = 'ApiError';
        this.errorCode = errorCode;
        this.errorValue = errorValue;
        this.errors = errors;
        this.correlationId = correlationId;
    }
}

export interface PaginationParams {
    page?: number;
    pageSize?: number;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
}
