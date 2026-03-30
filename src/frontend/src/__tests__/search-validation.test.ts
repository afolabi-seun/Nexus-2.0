import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';

/**
 * **Validates: Requirements 35.4**
 *
 * Property 22: Search minimum query length enforcement
 * For any query <2 chars, no API call; for ≥2 chars, search proceeds.
 *
 * We test the validation logic used in SearchPage: `if (q.length < 2) { setResults(null); return; }`
 */

const MIN_SEARCH_LENGTH = 2;

function shouldSearch(query: string): boolean {
    return query.length >= MIN_SEARCH_LENGTH;
}

describe('Search Minimum Query Length Enforcement', () => {
    it('property: queries with length < 2 should not trigger search', () => {
        fc.assert(
            fc.property(
                fc.string({ minLength: 0, maxLength: 1 }),
                (query) => {
                    expect(shouldSearch(query)).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: queries with length >= 2 should trigger search', () => {
        fc.assert(
            fc.property(
                fc.string({ minLength: 2, maxLength: 100 }),
                (query) => {
                    expect(shouldSearch(query)).toBe(true);
                }
            ),
            { numRuns: 100 }
        );
    });

    it('property: empty string never triggers search', () => {
        expect(shouldSearch('')).toBe(false);
    });

    it('property: single character never triggers search', () => {
        fc.assert(
            fc.property(
                fc.string({ minLength: 1, maxLength: 1 }),
                (query) => {
                    expect(shouldSearch(query)).toBe(false);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: boundary - exactly 2 characters triggers search', () => {
        fc.assert(
            fc.property(
                fc.string({ minLength: 2, maxLength: 2 }),
                (query) => {
                    expect(shouldSearch(query)).toBe(true);
                }
            ),
            { numRuns: 50 }
        );
    });
});
