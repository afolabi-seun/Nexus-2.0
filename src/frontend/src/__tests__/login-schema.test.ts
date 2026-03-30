import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { loginSchema } from '@/features/auth/schemas';

/**
 * **Validates: Requirements 2.9**
 *
 * Property 3: Login form validation schema
 * For any (email, password) pair, schema accepts iff email is valid format and password is non-empty.
 */

// Arbitrary for valid emails
const arbValidEmail = fc
    .tuple(
        fc.stringMatching(/^[a-z][a-z0-9]{0,9}$/),
        fc.stringMatching(/^[a-z][a-z0-9]{0,5}$/),
        fc.constantFrom('com', 'org', 'net', 'io')
    )
    .map(([user, domain, tld]) => `${user}@${domain}.${tld}`);

// Arbitrary for non-empty passwords
const arbNonEmptyPassword = fc.string({ minLength: 1, maxLength: 100 });

describe('Login Schema Validation', () => {
    it('property: accepts valid email + non-empty password', () => {
        fc.assert(
            fc.property(arbValidEmail, arbNonEmptyPassword, (email, password) => {
                const result = loginSchema.safeParse({ email, password });
                expect(result.success).toBe(true);
            }),
            { numRuns: 100 }
        );
    });

    it('property: rejects empty email', () => {
        fc.assert(
            fc.property(arbNonEmptyPassword, (password) => {
                const result = loginSchema.safeParse({ email: '', password });
                expect(result.success).toBe(false);
            }),
            { numRuns: 50 }
        );
    });

    it('property: rejects invalid email format (no @ sign)', () => {
        fc.assert(
            fc.property(
                fc.stringMatching(/^[a-zA-Z0-9]{1,20}$/).filter((s) => !s.includes('@')),
                arbNonEmptyPassword,
                (email, password) => {
                    const result = loginSchema.safeParse({ email, password });
                    expect(result.success).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: rejects empty password', () => {
        fc.assert(
            fc.property(arbValidEmail, (email) => {
                const result = loginSchema.safeParse({ email, password: '' });
                expect(result.success).toBe(false);
            }),
            { numRuns: 50 }
        );
    });
});
