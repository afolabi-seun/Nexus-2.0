import { describe, it, expect, beforeEach } from 'vitest';
import * as fc from 'fast-check';
import { useAuthStore } from '@/stores/authStore';
import type { AuthUser } from '@/types/auth';

/**
 * **Validates: Requirements 7.2**
 *
 * Property 8: Auth store state consistency
 * For any login/logout sequence, verify isAuthenticated, tokens, and user state.
 */

function makeUser(overrides: Partial<AuthUser> = {}): AuthUser {
    return {
        userId: 'user-1',
        organizationId: 'org-1',
        departmentId: 'dept-1',
        roleName: 'Member',
        email: 'test@example.com',
        firstName: 'Test',
        lastName: 'User',
        isFirstTimeUser: false,
        ...overrides,
    };
}

describe('Auth Store State Consistency', () => {
    beforeEach(() => {
        useAuthStore.getState().logout();
    });

    it('property: after login, isAuthenticated is true and tokens/user are set', () => {
        fc.assert(
            fc.property(
                fc.record({
                    accessToken: fc.string({ minLength: 10, maxLength: 100 }),
                    refreshToken: fc.string({ minLength: 10, maxLength: 100 }),
                    roleName: fc.constantFrom('OrgAdmin', 'DeptLead', 'Member', 'Viewer'),
                    isFirstTimeUser: fc.boolean(),
                }),
                ({ accessToken, refreshToken, roleName, isFirstTimeUser }) => {
                    const user = makeUser({ roleName, isFirstTimeUser });
                    useAuthStore.getState().login({ accessToken, refreshToken }, user);

                    const state = useAuthStore.getState();
                    expect(state.isAuthenticated).toBe(true);
                    expect(state.accessToken).toBe(accessToken);
                    expect(state.refreshToken).toBe(refreshToken);
                    expect(state.user).toEqual(user);
                    expect(state.isFirstTimeUser).toBe(isFirstTimeUser);

                    // Cleanup for next iteration
                    useAuthStore.getState().logout();
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: after logout, isAuthenticated is false and tokens/user are null', () => {
        fc.assert(
            fc.property(
                fc.record({
                    accessToken: fc.string({ minLength: 10, maxLength: 100 }),
                    refreshToken: fc.string({ minLength: 10, maxLength: 100 }),
                }),
                ({ accessToken, refreshToken }) => {
                    const user = makeUser();
                    useAuthStore.getState().login({ accessToken, refreshToken }, user);
                    useAuthStore.getState().logout();

                    const state = useAuthStore.getState();
                    expect(state.isAuthenticated).toBe(false);
                    expect(state.accessToken).toBeNull();
                    expect(state.refreshToken).toBeNull();
                    expect(state.user).toBeNull();
                    expect(state.isPlatformAdmin).toBe(false);
                    expect(state.isFirstTimeUser).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: isPlatformAdmin is true iff no organizationId and roleName is PlatformAdmin', () => {
        fc.assert(
            fc.property(
                fc.record({
                    organizationId: fc.option(fc.uuid(), { nil: null }),
                    roleName: fc.constantFrom('PlatformAdmin', 'OrgAdmin', 'DeptLead', 'Member', 'Viewer'),
                }),
                ({ organizationId, roleName }) => {
                    const user = makeUser({ organizationId, roleName });
                    useAuthStore.getState().login(
                        { accessToken: 'at', refreshToken: 'rt' },
                        user
                    );

                    const state = useAuthStore.getState();
                    const expected = !organizationId && roleName === 'PlatformAdmin';
                    expect(state.isPlatformAdmin).toBe(expected);

                    useAuthStore.getState().logout();
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: refreshTokens updates tokens without changing user or auth state', () => {
        const user = makeUser();
        useAuthStore.getState().login({ accessToken: 'old-at', refreshToken: 'old-rt' }, user);

        fc.assert(
            fc.property(
                fc.string({ minLength: 10, maxLength: 100 }),
                fc.string({ minLength: 10, maxLength: 100 }),
                (newAt, newRt) => {
                    useAuthStore.getState().refreshTokens(newAt, newRt);

                    const state = useAuthStore.getState();
                    expect(state.accessToken).toBe(newAt);
                    expect(state.refreshToken).toBe(newRt);
                    expect(state.isAuthenticated).toBe(true);
                    expect(state.user).toEqual(user);
                }
            ),
            { numRuns: 50 }
        );
    });
});
