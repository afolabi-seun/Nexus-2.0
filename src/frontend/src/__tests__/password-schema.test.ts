import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { passwordSchema } from '@/features/auth/schemas';

/**
 * **Validates: Requirements 4.6**
 *
 * Property 5: Password complexity validation schema
 * For any string, schema accepts iff ≥8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char.
 */

// Generator for valid passwords: guaranteed to have uppercase, lowercase, digit, special
const arbValidPassword = fc
    .tuple(
        fc.stringMatching(/^[A-Z]$/),
        fc.stringMatching(/^[a-z]$/),
        fc.stringMatching(/^[0-9]$/),
        fc.constantFrom('!', '@', '#', '$', '%', '^', '&', '*'),
        fc.stringMatching(/^[A-Za-z0-9!@#$%^&*]{4,20}$/)
    )
    .map(([upper, lower, digit, special, rest]) => `${upper}${lower}${digit}${special}${rest}`);

describe('Password Complexity Validation', () => {
    it('property: accepts passwords meeting all complexity requirements', () => {
        fc.assert(
            fc.property(arbValidPassword, (password) => {
                const result = passwordSchema.safeParse({
                    newPassword: password,
                    confirmPassword: password,
                });
                expect(result.success).toBe(true);
            }),
            { numRuns: 100 }
        );
    });

    it('property: rejects passwords shorter than 8 characters', () => {
        fc.assert(
            fc.property(
                fc.string({ minLength: 1, maxLength: 7 }),
                (password) => {
                    const result = passwordSchema.safeParse({
                        newPassword: password,
                        confirmPassword: password,
                    });
                    expect(result.success).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: rejects passwords without uppercase letter', () => {
        fc.assert(
            fc.property(
                fc.stringMatching(/^[a-z0-9!@#$%^&*]{8,20}$/),
                (password) => {
                    const result = passwordSchema.safeParse({
                        newPassword: password,
                        confirmPassword: password,
                    });
                    expect(result.success).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: rejects passwords without lowercase letter', () => {
        fc.assert(
            fc.property(
                fc.stringMatching(/^[A-Z0-9!@#$%^&*]{8,20}$/),
                (password) => {
                    const result = passwordSchema.safeParse({
                        newPassword: password,
                        confirmPassword: password,
                    });
                    expect(result.success).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: rejects passwords without digit', () => {
        fc.assert(
            fc.property(
                fc.stringMatching(/^[A-Za-z!@#$%^&*]{8,20}$/),
                (password) => {
                    const result = passwordSchema.safeParse({
                        newPassword: password,
                        confirmPassword: password,
                    });
                    expect(result.success).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: rejects passwords without special character', () => {
        fc.assert(
            fc.property(
                fc.stringMatching(/^[A-Za-z0-9]{8,20}$/),
                (password) => {
                    const result = passwordSchema.safeParse({
                        newPassword: password,
                        confirmPassword: password,
                    });
                    expect(result.success).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: rejects when confirmPassword does not match', () => {
        fc.assert(
            fc.property(
                arbValidPassword,
                arbValidPassword.filter((p) => p.length > 0),
                (password, other) => {
                    fc.pre(password !== other);
                    const result = passwordSchema.safeParse({
                        newPassword: password,
                        confirmPassword: other,
                    });
                    expect(result.success).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });
});
