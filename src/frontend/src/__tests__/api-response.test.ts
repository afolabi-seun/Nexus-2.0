import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { ApiError } from '@/types/api';
import type { ApiResponse } from '@/types/api';

/**
 * **Validates: Requirements 1.4**
 *
 * Property 2: ApiResponse parsing extracts data or throws typed error
 * For any ApiResponse, success (no errorCode) returns data, error (with errorCode) throws ApiError.
 */

function parseApiResponse<T>(body: ApiResponse<T>): T {
    if (body.errorCode) {
        throw new ApiError(
            body.message ?? 'An error occurred',
            body.errorCode,
            body.errorValue ?? 0,
            body.errors,
            body.correlationId
        );
    }
    return body.data as T;
}

const arbErrorCode = fc.constantFrom(
    'INVALID_CREDENTIALS',
    'ACCOUNT_LOCKED',
    'VALIDATION_ERROR',
    'INTERNAL_ERROR',
    'NETWORK_ERROR'
);

describe('ApiResponse Parsing', () => {
    it('property: success response extracts data correctly', () => {
        fc.assert(
            fc.property(
                fc.record({
                    responseCode: fc.constant('00'),
                    responseDescription: fc.string(),
                    data: fc.anything(),
                    errorCode: fc.constant(null),
                    errorValue: fc.constant(null),
                    message: fc.constant(null),
                    correlationId: fc.option(fc.uuid(), { nil: null }),
                    errors: fc.constant(null),
                }),
                (response) => {
                    const result = parseApiResponse(response as ApiResponse<unknown>);
                    expect(result).toEqual(response.data);
                }
            ),
            { numRuns: 100 }
        );
    });

    it('property: error response throws ApiError with correct fields', () => {
        fc.assert(
            fc.property(
                fc.record({
                    responseCode: fc.constant('99'),
                    responseDescription: fc.string(),
                    data: fc.constant(null),
                    errorCode: arbErrorCode,
                    errorValue: fc.integer({ min: 0, max: 999 }),
                    message: fc.option(fc.string({ minLength: 1 }), { nil: null }),
                    correlationId: fc.option(fc.uuid(), { nil: null }),
                    errors: fc.constant(null),
                }),
                (response) => {
                    try {
                        parseApiResponse(response as ApiResponse<unknown>);
                        expect.unreachable('Should have thrown');
                    } catch (err) {
                        expect(err).toBeInstanceOf(ApiError);
                        const apiErr = err as ApiError;
                        expect(apiErr.errorCode).toBe(response.errorCode);
                        expect(apiErr.errorValue).toBe(response.errorValue);
                        expect(apiErr.correlationId).toBe(response.correlationId);
                    }
                }
            ),
            { numRuns: 100 }
        );
    });

    it('property: error response uses fallback message when message is null', () => {
        const response: ApiResponse<unknown> = {
            responseCode: '99',
            responseDescription: 'Error',
            data: null,
            errorCode: 'INTERNAL_ERROR',
            errorValue: 0,
            message: null,
            correlationId: null,
            errors: null,
        };

        try {
            parseApiResponse(response);
            expect.unreachable('Should have thrown');
        } catch (err) {
            expect(err).toBeInstanceOf(ApiError);
            expect((err as ApiError).message).toBe('An error occurred');
        }
    });
});
