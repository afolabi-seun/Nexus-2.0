import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { otpSchema } from '@/features/auth/schemas';

/**
 * **Validates: Requirements 5.8**
 *
 * Property 6: OTP validation schema
 * For any string, schema accepts iff exactly 6 digit characters.
 */

// Arbitrary for valid 6-digit OTPs
const arbValidOtp = fc.stringMatching(/^[0-9]{6}$/);

describe('OTP Validation Schema', () => {
    it('property: accepts exactly 6-digit strings', () => {
        fc.assert(
            fc.property(arbValidOtp, (otp) => {
                const result = otpSchema.safeParse({ otp });
                expect(result.success).toBe(true);
            }),
            { numRuns: 100 }
        );
    });

    it('property: rejects strings shorter than 6 characters', () => {
        fc.assert(
            fc.property(
                fc.stringMatching(/^[0-9]{1,5}$/),
                (otp) => {
                    const result = otpSchema.safeParse({ otp });
                    expect(result.success).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: rejects strings longer than 6 characters', () => {
        fc.assert(
            fc.property(
                fc.stringMatching(/^[0-9]{7,12}$/),
                (otp) => {
                    const result = otpSchema.safeParse({ otp });
                    expect(result.success).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: rejects 6-character strings containing non-digit characters', () => {
        fc.assert(
            fc.property(
                fc.stringMatching(/^[a-zA-Z!@#$%^&*]{6}$/),
                (otp) => {
                    const result = otpSchema.safeParse({ otp });
                    expect(result.success).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: rejects empty string', () => {
        const result = otpSchema.safeParse({ otp: '' });
        expect(result.success).toBe(false);
    });
});
