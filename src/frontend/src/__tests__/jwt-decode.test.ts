import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { extractUserFromToken } from '@/stores/authStore';

/**
 * **Validates: Requirements 7.3, 3.5**
 *
 * Property 4: JWT decode extracts user claims
 * For any valid JWT payload, decode produces correct AuthUser;
 * PlatformAdmin detection when no organizationId.
 */

function encodeJwtPayload(payload: Record<string, unknown>): string {
    const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
    const body = btoa(JSON.stringify(payload));
    const sig = btoa('fake-signature');
    return `${header}.${body}.${sig}`;
}

const arbUuid = fc.uuid();

describe('JWT Decode - extractUserFromToken', () => {
    it('property: extracts all user claims from JWT payload', () => {
        fc.assert(
            fc.property(
                fc.record({
                    sub: arbUuid,
                    organizationId: fc.option(arbUuid, { nil: undefined }),
                    departmentId: fc.option(arbUuid, { nil: undefined }),
                    roleName: fc.constantFrom('OrgAdmin', 'DeptLead', 'Member', 'Viewer'),
                    email: fc.emailAddress(),
                    firstName: fc.stringMatching(/^[A-Za-z]{1,20}$/),
                    lastName: fc.stringMatching(/^[A-Za-z]{1,20}$/),
                    isFirstTimeUser: fc.boolean(),
                }),
                (payload) => {
                    const token = encodeJwtPayload(payload);
                    const user = extractUserFromToken(token);

                    expect(user.userId).toBe(payload.sub);
                    expect(user.organizationId).toBe(payload.organizationId ?? null);
                    expect(user.departmentId).toBe(payload.departmentId ?? null);
                    expect(user.roleName).toBe(payload.roleName);
                    expect(user.email).toBe(payload.email);
                    expect(user.firstName).toBe(payload.firstName);
                    expect(user.lastName).toBe(payload.lastName);
                    expect(user.isFirstTimeUser).toBe(payload.isFirstTimeUser);
                }
            ),
            { numRuns: 100 }
        );
    });

    it('property: PlatformAdmin detected when no organizationId and role is PlatformAdmin', () => {
        fc.assert(
            fc.property(
                fc.record({
                    sub: arbUuid,
                    roleName: fc.constant('PlatformAdmin'),
                    email: fc.emailAddress(),
                    firstName: fc.stringMatching(/^[A-Za-z]{1,10}$/),
                    lastName: fc.stringMatching(/^[A-Za-z]{1,10}$/),
                    isFirstTimeUser: fc.boolean(),
                }),
                (payload) => {
                    const token = encodeJwtPayload(payload);
                    const user = extractUserFromToken(token);

                    expect(user.organizationId).toBeNull();
                    expect(user.roleName).toBe('PlatformAdmin');
                    // PlatformAdmin is detected by: no organizationId + roleName === 'PlatformAdmin'
                    const isPlatformAdmin = !user.organizationId && user.roleName === 'PlatformAdmin';
                    expect(isPlatformAdmin).toBe(true);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: non-PlatformAdmin with organizationId is not detected as PlatformAdmin', () => {
        fc.assert(
            fc.property(
                fc.record({
                    sub: arbUuid,
                    organizationId: arbUuid,
                    roleName: fc.constantFrom('OrgAdmin', 'DeptLead', 'Member', 'Viewer'),
                    email: fc.emailAddress(),
                    firstName: fc.stringMatching(/^[A-Za-z]{1,10}$/),
                    lastName: fc.stringMatching(/^[A-Za-z]{1,10}$/),
                    isFirstTimeUser: fc.boolean(),
                }),
                (payload) => {
                    const token = encodeJwtPayload(payload);
                    const user = extractUserFromToken(token);

                    const isPlatformAdmin = !user.organizationId && user.roleName === 'PlatformAdmin';
                    expect(isPlatformAdmin).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });
});
