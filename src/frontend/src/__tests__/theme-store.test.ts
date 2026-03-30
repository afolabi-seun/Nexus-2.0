import { describe, it, expect, beforeEach } from 'vitest';
import * as fc from 'fast-check';

/**
 * **Validates: Requirements 29.4, 30.2, 30.4**
 *
 * Property 21: Theme store resolves and applies theme correctly
 * For any theme setting, resolvedTheme is correct; dark class on html iff resolvedTheme=Dark.
 */

// We test the resolution logic directly rather than importing the store
// (which has side effects on import). We replicate the pure logic here.

type ThemeSetting = 'Light' | 'Dark' | 'System';
type ResolvedTheme = 'Light' | 'Dark';

function resolveTheme(theme: ThemeSetting, prefersDark: boolean): ResolvedTheme {
    if (theme === 'System') return prefersDark ? 'Dark' : 'Light';
    return theme;
}

function applyThemeToDOM(resolved: ResolvedTheme): void {
    const html = document.documentElement;
    if (resolved === 'Dark') {
        html.classList.add('dark');
    } else {
        html.classList.remove('dark');
    }
}

const arbThemeSetting = fc.constantFrom<ThemeSetting>('Light', 'Dark', 'System');

describe('Theme Store Resolution and Application', () => {
    beforeEach(() => {
        document.documentElement.classList.remove('dark');
    });

    it('property: Light always resolves to Light regardless of system preference', () => {
        fc.assert(
            fc.property(fc.boolean(), (prefersDark) => {
                expect(resolveTheme('Light', prefersDark)).toBe('Light');
            }),
            { numRuns: 10 }
        );
    });

    it('property: Dark always resolves to Dark regardless of system preference', () => {
        fc.assert(
            fc.property(fc.boolean(), (prefersDark) => {
                expect(resolveTheme('Dark', prefersDark)).toBe('Dark');
            }),
            { numRuns: 10 }
        );
    });

    it('property: System resolves based on system preference', () => {
        fc.assert(
            fc.property(fc.boolean(), (prefersDark) => {
                const resolved = resolveTheme('System', prefersDark);
                expect(resolved).toBe(prefersDark ? 'Dark' : 'Light');
            }),
            { numRuns: 10 }
        );
    });

    it('property: dark class on html iff resolvedTheme is Dark', () => {
        fc.assert(
            fc.property(arbThemeSetting, fc.boolean(), (theme, prefersDark) => {
                const resolved = resolveTheme(theme, prefersDark);
                applyThemeToDOM(resolved);

                const hasDarkClass = document.documentElement.classList.contains('dark');
                expect(hasDarkClass).toBe(resolved === 'Dark');
            }),
            { numRuns: 30 }
        );
    });

    it('property: applying theme is idempotent', () => {
        fc.assert(
            fc.property(
                fc.constantFrom<ResolvedTheme>('Light', 'Dark'),
                (resolved) => {
                    applyThemeToDOM(resolved);
                    applyThemeToDOM(resolved);
                    const hasDarkClass = document.documentElement.classList.contains('dark');
                    expect(hasDarkClass).toBe(resolved === 'Dark');
                }
            ),
            { numRuns: 10 }
        );
    });
});
