import { describe, it, expect, vi } from 'vitest';
import * as fc from 'fast-check';
import { renderHook, act } from '@testing-library/react';
import { createElement } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { useListFilters, serializeFilters, deserializeFilters } from '@/hooks/useListFilters';
import type { FilterConfig, FilterFieldType, FilterValues } from '@/types/filters';

/**
 * Feature: standardized-filters
 *
 * Property-based tests for the useListFilters hook.
 */

// --- Generators ---

const NON_DEBOUNCED_TYPES: FilterFieldType[] = ['select', 'multi-select', 'date'];
const ALL_TYPES: FilterFieldType[] = ['select', 'multi-select', 'text-search', 'date', 'date-range', 'async-search'];

const arbFilterFieldType = fc.constantFrom<FilterFieldType>(...ALL_TYPES);
const arbNonDebouncedType = fc.constantFrom<FilterFieldType>(...NON_DEBOUNCED_TYPES);

/** Generate a valid filter key (alphanumeric, no commas or special chars) */
const arbFilterKey = fc.stringMatching(/^[a-zA-Z][a-zA-Z0-9]{0,19}$/).filter((s) => s.length > 0);

/** Generate a non-empty filter value string (no commas to avoid multi-select ambiguity in simple string values) */
const arbFilterValue = fc.stringMatching(/^[a-zA-Z0-9 _-]{1,30}$/).filter((s) => s.trim().length > 0);

/** Generate a FilterConfig with a non-debounced type for immediate update tests */
function arbFilterConfig(type?: fc.Arbitrary<FilterFieldType>): fc.Arbitrary<FilterConfig> {
    return fc.record({
        key: arbFilterKey,
        label: fc.string({ minLength: 1, maxLength: 30 }),
        type: type ?? arbFilterFieldType,
    });
}

/** Generate a list of FilterConfigs with unique keys */
function arbFilterConfigs(
    minLength = 1,
    maxLength = 5,
    type?: fc.Arbitrary<FilterFieldType>
): fc.Arbitrary<FilterConfig[]> {
    return fc
        .uniqueArray(arbFilterConfig(type), { minLength, maxLength, selector: (c) => c.key })
        .filter((arr) => arr.length >= minLength);
}

/** MemoryRouter wrapper for renderHook */
function wrapper({ children }: { children: React.ReactNode }) {
    return createElement(MemoryRouter, null, children);
}

// --- Property Tests ---

describe('useListFilters – Property Tests', () => {
    /**
     * Feature: standardized-filters, Property 1: Filter value round-trip
     *
     * updateFilter(key, val) then reading filterValues[key] returns val;
     * updateFilter(key, undefined) removes the key.
     *
     * **Validates: Requirements 2.2, 2.3**
     */
    describe('Property 1: Filter value round-trip', () => {
        it('setting a filter value and reading it back returns the same value', () => {
            fc.assert(
                fc.property(
                    arbFilterConfigs(1, 5, arbNonDebouncedType),
                    arbFilterValue,
                    (configs, value) => {
                        const config = configs[0];
                        const { result } = renderHook(
                            () => useListFilters(configs, { syncToUrl: false }),
                            { wrapper }
                        );

                        act(() => {
                            result.current.updateFilter(config.key, value);
                        });

                        if (config.type === 'multi-select') {
                            expect(result.current.filterValues[config.key]).toEqual(value);
                        } else {
                            expect(result.current.filterValues[config.key]).toBe(value);
                        }
                    }
                ),
                { numRuns: 100 }
            );
        });

        it('setting a filter value to undefined removes the key', () => {
            fc.assert(
                fc.property(
                    arbFilterConfigs(1, 5, arbNonDebouncedType),
                    arbFilterValue,
                    (configs, value) => {
                        const config = configs[0];
                        const { result } = renderHook(
                            () => useListFilters(configs, { syncToUrl: false }),
                            { wrapper }
                        );

                        // Set then remove
                        act(() => {
                            result.current.updateFilter(config.key, value);
                        });
                        act(() => {
                            result.current.updateFilter(config.key, undefined);
                        });

                        expect(result.current.filterValues[config.key]).toBeUndefined();
                        expect(config.key in result.current.filterValues).toBe(false);
                    }
                ),
                { numRuns: 100 }
            );
        });
    });

    /**
     * Feature: standardized-filters, Property 2: Clear filters idempotency
     *
     * clearFilters() always produces an empty record regardless of prior state.
     *
     * **Validates: Requirements 2.4**
     */
    describe('Property 2: Clear filters idempotency', () => {
        it('clearFilters always produces an empty record', () => {
            fc.assert(
                fc.property(
                    arbFilterConfigs(1, 5, arbNonDebouncedType),
                    fc.array(arbFilterValue, { minLength: 1, maxLength: 5 }),
                    (configs, values) => {
                        const { result } = renderHook(
                            () => useListFilters(configs, { syncToUrl: false }),
                            { wrapper }
                        );

                        // Set some filters
                        act(() => {
                            configs.forEach((config, i) => {
                                const val = values[i % values.length];
                                result.current.updateFilter(config.key, val);
                            });
                        });

                        // Clear
                        act(() => {
                            result.current.clearFilters();
                        });

                        expect(result.current.filterValues).toEqual({});
                        expect(result.current.hasActiveFilters).toBe(false);
                    }
                ),
                { numRuns: 100 }
            );
        });

        it('calling clearFilters twice is the same as calling it once', () => {
            fc.assert(
                fc.property(
                    arbFilterConfigs(1, 3, arbNonDebouncedType),
                    arbFilterValue,
                    (configs, value) => {
                        const { result } = renderHook(
                            () => useListFilters(configs, { syncToUrl: false }),
                            { wrapper }
                        );

                        act(() => {
                            result.current.updateFilter(configs[0].key, value);
                        });

                        act(() => {
                            result.current.clearFilters();
                        });
                        const afterFirst = { ...result.current.filterValues };

                        act(() => {
                            result.current.clearFilters();
                        });
                        const afterSecond = { ...result.current.filterValues };

                        expect(afterFirst).toEqual(afterSecond);
                        expect(afterSecond).toEqual({});
                    }
                ),
                { numRuns: 100 }
            );
        });
    });

    /**
     * Feature: standardized-filters, Property 3: URL sync round-trip
     *
     * Serializing FilterValues to URL params and deserializing back yields
     * the original values (when syncToUrl is true).
     *
     * **Validates: Requirements 3.1, 3.2**
     */
    describe('Property 3: URL sync round-trip', () => {
        it('serialize then deserialize produces equivalent FilterValues', () => {
            // Generate configs with unique keys, then generate matching values
            const arbConfigsAndValues = arbFilterConfigs(1, 5).chain((configs) => {
                // Build a FilterValues record matching the configs
                const valueArbs: Record<string, fc.Arbitrary<string | string[]>> = {};
                for (const config of configs) {
                    if (config.type === 'multi-select') {
                        valueArbs[config.key] = fc.uniqueArray(arbFilterValue, {
                            minLength: 1,
                            maxLength: 3,
                        });
                    } else {
                        valueArbs[config.key] = arbFilterValue;
                    }
                }
                return fc.record(valueArbs).map((values) => ({ configs, values: values as FilterValues }));
            });

            fc.assert(
                fc.property(arbConfigsAndValues, ({ configs, values }) => {
                    const serialized = serializeFilters(values);
                    const params = new URLSearchParams(serialized);
                    const deserialized = deserializeFilters(params, configs);

                    // For each key in the original values, the deserialized value should match
                    for (const config of configs) {
                        const original = values[config.key];
                        const restored = deserialized[config.key];

                        if (original === undefined || original === '') {
                            expect(restored).toBeUndefined();
                        } else if (Array.isArray(original)) {
                            if (config.type === 'multi-select') {
                                expect(restored).toEqual(original);
                            } else {
                                // Arrays serialized as comma-joined string
                                expect(restored).toBe(original.join(','));
                            }
                        } else {
                            if (config.type === 'multi-select') {
                                // A single string value deserialized as array for multi-select
                                expect(restored).toEqual([original]);
                            } else {
                                expect(restored).toBe(original);
                            }
                        }
                    }
                }),
                { numRuns: 100 }
            );
        });
    });

    /**
     * Feature: standardized-filters, Property 4: Page reset on filter change
     *
     * Every updateFilter call triggers onPageReset.
     *
     * **Validates: Requirements 2.5**
     */
    describe('Property 4: Page reset on filter change', () => {
        it('updateFilter always triggers onPageReset for non-debounced types', () => {
            fc.assert(
                fc.property(
                    arbFilterConfigs(1, 5, arbNonDebouncedType),
                    arbFilterValue,
                    (configs, value) => {
                        const onPageReset = vi.fn();
                        const { result } = renderHook(
                            () => useListFilters(configs, { syncToUrl: false, onPageReset }),
                            { wrapper }
                        );

                        act(() => {
                            result.current.updateFilter(configs[0].key, value);
                        });

                        expect(onPageReset).toHaveBeenCalledTimes(1);
                    }
                ),
                { numRuns: 100 }
            );
        });

        it('updateFilter triggers onPageReset for debounced types after timer flush', () => {
            fc.assert(
                fc.property(
                    arbFilterConfigs(
                        1,
                        3,
                        fc.constantFrom<FilterFieldType>('text-search', 'async-search')
                    ),
                    arbFilterValue,
                    (configs, value) => {
                        vi.useFakeTimers();
                        const onPageReset = vi.fn();
                        const { result, unmount } = renderHook(
                            () => useListFilters(configs, { syncToUrl: false, onPageReset }),
                            { wrapper }
                        );

                        act(() => {
                            result.current.updateFilter(configs[0].key, value);
                        });

                        // Not yet called due to debounce
                        expect(onPageReset).not.toHaveBeenCalled();

                        // Flush debounce timer
                        act(() => {
                            vi.advanceTimersByTime(300);
                        });

                        expect(onPageReset).toHaveBeenCalledTimes(1);

                        unmount();
                        vi.useRealTimers();
                    }
                ),
                { numRuns: 100 }
            );
        });
    });
});


// --- Unit Tests ---

describe('useListFilters – Unit Tests', () => {
    /**
     * Debounce behavior for text-search and async-search fields.
     * **Validates: Requirements 2.6**
     */
    describe('debounce behavior', () => {
        it('text-search value is NOT updated before 300ms and IS updated after', () => {
            vi.useFakeTimers();

            const configs: FilterConfig[] = [
                { key: 'search', label: 'Search', type: 'text-search' },
            ];

            const { result } = renderHook(
                () => useListFilters(configs, { syncToUrl: false }),
                { wrapper }
            );

            act(() => {
                result.current.updateFilter('search', 'hello');
            });

            // Value should NOT be propagated yet
            expect(result.current.filterValues['search']).toBeUndefined();

            // Advance past the debounce window
            act(() => {
                vi.advanceTimersByTime(300);
            });

            // Now the value should be propagated
            expect(result.current.filterValues['search']).toBe('hello');

            vi.useRealTimers();
        });

        it('async-search value is NOT updated before 300ms and IS updated after', () => {
            vi.useFakeTimers();

            const configs: FilterConfig[] = [
                {
                    key: 'assignee',
                    label: 'Assignee',
                    type: 'async-search',
                    loadOptions: async () => [],
                },
            ];

            const { result } = renderHook(
                () => useListFilters(configs, { syncToUrl: false }),
                { wrapper }
            );

            act(() => {
                result.current.updateFilter('assignee', 'jane');
            });

            expect(result.current.filterValues['assignee']).toBeUndefined();

            act(() => {
                vi.advanceTimersByTime(300);
            });

            expect(result.current.filterValues['assignee']).toBe('jane');

            vi.useRealTimers();
        });

        it('select value is updated immediately without debounce', () => {
            vi.useFakeTimers();

            const configs: FilterConfig[] = [
                {
                    key: 'status',
                    label: 'Status',
                    type: 'select',
                    options: [{ value: 'active', label: 'Active' }],
                },
            ];

            const { result } = renderHook(
                () => useListFilters(configs, { syncToUrl: false }),
                { wrapper }
            );

            act(() => {
                result.current.updateFilter('status', 'active');
            });

            // Should be set immediately — no debounce for select
            expect(result.current.filterValues['status']).toBe('active');

            vi.useRealTimers();
        });
    });

    /**
     * syncToUrl: false keeps URL unchanged.
     * **Validates: Requirements 3.3, 3.4**
     */
    describe('syncToUrl: false', () => {
        it('does not modify URL search params when syncToUrl is false', () => {
            const configs: FilterConfig[] = [
                {
                    key: 'status',
                    label: 'Status',
                    type: 'select',
                    options: [{ value: 'active', label: 'Active' }],
                },
            ];

            const { result } = renderHook(
                () => useListFilters(configs, { syncToUrl: false }),
                { wrapper }
            );

            act(() => {
                result.current.updateFilter('status', 'active');
            });

            // URL should remain empty since syncToUrl is false
            expect(window.location.search).toBe('');
        });
    });

    /**
     * hasActiveFilters returns correct boolean.
     * **Validates: Requirements 3.4 (local state), 2.1 (hasActiveFilters)**
     */
    describe('hasActiveFilters', () => {
        it('returns false when no filters are set', () => {
            const configs: FilterConfig[] = [
                { key: 'status', label: 'Status', type: 'select' },
            ];

            const { result } = renderHook(
                () => useListFilters(configs, { syncToUrl: false }),
                { wrapper }
            );

            expect(result.current.hasActiveFilters).toBe(false);
        });

        it('returns true when a filter is set', () => {
            const configs: FilterConfig[] = [
                { key: 'status', label: 'Status', type: 'select' },
            ];

            const { result } = renderHook(
                () => useListFilters(configs, { syncToUrl: false }),
                { wrapper }
            );

            act(() => {
                result.current.updateFilter('status', 'active');
            });

            expect(result.current.hasActiveFilters).toBe(true);
        });

        it('returns false after clearing filters', () => {
            const configs: FilterConfig[] = [
                { key: 'status', label: 'Status', type: 'select' },
            ];

            const { result } = renderHook(
                () => useListFilters(configs, { syncToUrl: false }),
                { wrapper }
            );

            act(() => {
                result.current.updateFilter('status', 'active');
            });
            expect(result.current.hasActiveFilters).toBe(true);

            act(() => {
                result.current.clearFilters();
            });
            expect(result.current.hasActiveFilters).toBe(false);
        });

        it('returns false when filter is set to empty string', () => {
            const configs: FilterConfig[] = [
                { key: 'search', label: 'Search', type: 'select' },
            ];

            const { result } = renderHook(
                () => useListFilters(configs, { syncToUrl: false }),
                { wrapper }
            );

            act(() => {
                result.current.updateFilter('search', '');
            });

            expect(result.current.hasActiveFilters).toBe(false);
        });
    });
});


// --- Integration Property Tests ---

describe('useListFilters – Integration Property Tests', () => {
    /**
     * Feature: standardized-filters, Property 7: Saved filter apply round-trip
     *
     * Applying a saved filter (by calling updateFilter for each key) sets
     * FilterValues to match the saved filter's stored values.
     * Uses non-debounced types (select, multi-select) for immediate verification.
     *
     * **Validates: Requirements 5.4**
     */
    describe('Property 7: Saved filter apply round-trip', () => {
        it('applying a saved filter via updateFilter sets filterValues to match', () => {
            fc.assert(
                fc.property(
                    arbFilterConfigs(1, 5, arbNonDebouncedType).chain((configs) => {
                        // Generate a saved filter record matching the configs
                        const valueArbs: Record<string, fc.Arbitrary<string | string[]>> = {};
                        for (const config of configs) {
                            if (config.type === 'multi-select') {
                                valueArbs[config.key] = fc.uniqueArray(arbFilterValue, {
                                    minLength: 1,
                                    maxLength: 3,
                                });
                            } else {
                                valueArbs[config.key] = arbFilterValue;
                            }
                        }
                        return fc.record(valueArbs).map((savedValues) => ({
                            configs,
                            savedValues: savedValues as FilterValues,
                        }));
                    }),
                    ({ configs, savedValues }) => {
                        const { result } = renderHook(
                            () => useListFilters(configs, { syncToUrl: false }),
                            { wrapper }
                        );

                        // Simulate applying a saved filter by calling updateFilter for each key
                        act(() => {
                            for (const [key, value] of Object.entries(savedValues)) {
                                result.current.updateFilter(key, value);
                            }
                        });

                        // Verify filterValues matches the saved filter
                        for (const [key, value] of Object.entries(savedValues)) {
                            expect(result.current.filterValues[key]).toEqual(value);
                        }
                    }
                ),
                { numRuns: 100 }
            );
        });
    });

    /**
     * Feature: standardized-filters, Property 8: Debounce timing
     *
     * text-search and async-search values are not propagated before 300ms.
     *
     * **Validates: Requirements 2.6**
     */
    describe('Property 8: Debounce timing', () => {
        it('debounced filter values are NOT in filterValues before 300ms and ARE after', () => {
            fc.assert(
                fc.property(
                    arbFilterConfigs(
                        1,
                        3,
                        fc.constantFrom<FilterFieldType>('text-search', 'async-search')
                    ),
                    arbFilterValue,
                    (configs, value) => {
                        vi.useFakeTimers();
                        const { result, unmount } = renderHook(
                            () => useListFilters(configs, { syncToUrl: false }),
                            { wrapper }
                        );

                        const targetKey = configs[0].key;

                        act(() => {
                            result.current.updateFilter(targetKey, value);
                        });

                        // Value should NOT be propagated before 300ms
                        expect(result.current.filterValues[targetKey]).toBeUndefined();

                        // Advance to just before the debounce threshold
                        act(() => {
                            vi.advanceTimersByTime(299);
                        });
                        expect(result.current.filterValues[targetKey]).toBeUndefined();

                        // Advance past the debounce threshold
                        act(() => {
                            vi.advanceTimersByTime(1);
                        });

                        // Value should now be propagated
                        expect(result.current.filterValues[targetKey]).toBe(value);

                        unmount();
                        vi.useRealTimers();
                    }
                ),
                { numRuns: 100 }
            );
        });
    });

    /**
     * Feature: standardized-filters, Property 9: hasActiveFilters consistency
     *
     * `hasActiveFilters` is `true` iff at least one key in FilterValues
     * has a non-empty value.
     *
     * **Validates: Requirements 2.1**
     */
    describe('Property 9: hasActiveFilters consistency', () => {
        it('hasActiveFilters matches whether any filter has a non-empty value', () => {
            fc.assert(
                fc.property(
                    arbFilterConfigs(1, 5, arbNonDebouncedType).chain((configs) => {
                        // For each config, randomly decide if it gets a value or stays empty
                        const boolArbs = configs.map(() => fc.boolean());
                        return fc.tuple(...boolArbs).chain((hasValues) => {
                            const valueArbs = configs.map((cfg, i) => {
                                if (!hasValues[i]) return fc.constant(undefined as string | string[] | undefined);
                                if (cfg.type === 'multi-select') {
                                    return fc.uniqueArray(arbFilterValue, {
                                        minLength: 1,
                                        maxLength: 3,
                                    }).map((arr) => arr as string | string[] | undefined);
                                }
                                return arbFilterValue.map((v) => v as string | string[] | undefined);
                            });
                            return fc.tuple(...valueArbs).map((vals) => ({
                                configs,
                                valuePairs: configs.map((cfg, i) => ({
                                    key: cfg.key,
                                    value: vals[i],
                                })),
                            }));
                        });
                    }),
                    ({ configs, valuePairs }) => {
                        const { result } = renderHook(
                            () => useListFilters(configs, { syncToUrl: false }),
                            { wrapper }
                        );

                        // Apply all filter values
                        act(() => {
                            for (const { key, value } of valuePairs) {
                                result.current.updateFilter(key, value);
                            }
                        });

                        // Compute expected hasActiveFilters
                        const expectedHasActive = valuePairs.some(({ value }) => {
                            if (value === undefined || value === '') return false;
                            if (Array.isArray(value) && value.length === 0) return false;
                            return true;
                        });

                        expect(result.current.hasActiveFilters).toBe(expectedHasActive);
                    }
                ),
                { numRuns: 100 }
            );
        });
    });
});
