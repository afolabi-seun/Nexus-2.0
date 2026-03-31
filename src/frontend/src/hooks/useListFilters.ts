import { useState, useCallback, useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import type { FilterConfig, FilterValues } from '@/types/filters';

interface UseListFiltersOptions {
    syncToUrl?: boolean;
    onPageReset?: () => void;
}

interface UseListFiltersReturn {
    filterValues: FilterValues;
    updateFilter: (key: string, value: string | string[] | undefined) => void;
    clearFilters: () => void;
    hasActiveFilters: boolean;
    activeFilterCount: number;
}

const DEBOUNCED_TYPES = new Set(['text-search', 'async-search']);
const DEBOUNCE_MS = 300;

/**
 * Serialize FilterValues into URLSearchParams-compatible entries.
 * Array values are joined with commas; undefined/empty values are omitted.
 */
export function serializeFilters(values: FilterValues): Record<string, string> {
    const params: Record<string, string> = {};
    for (const [key, val] of Object.entries(values)) {
        if (val === undefined || val === '') continue;
        if (Array.isArray(val)) {
            if (val.length > 0) {
                params[key] = val.join(',');
            }
        } else {
            params[key] = val;
        }
    }
    return params;
}

/**
 * Deserialize URLSearchParams into FilterValues using FilterConfig
 * to determine which keys are arrays (multi-select) vs strings.
 */
export function deserializeFilters(
    searchParams: URLSearchParams,
    configs: FilterConfig[]
): FilterValues {
    const values: FilterValues = {};
    const configMap = new Map(configs.map((c) => [c.key, c]));

    for (const [key, raw] of searchParams.entries()) {
        const config = configMap.get(key);
        if (!config) continue;

        if (config.type === 'multi-select') {
            const arr = raw.split(',').filter(Boolean);
            if (arr.length > 0) {
                values[key] = arr;
            }
        } else {
            if (raw) {
                values[key] = raw;
            }
        }
    }
    return values;
}

function isValueEmpty(value: string | string[] | undefined): boolean {
    if (value === undefined || value === '') return true;
    if (Array.isArray(value) && value.length === 0) return true;
    return false;
}

function countActiveFilters(values: FilterValues): number {
    let count = 0;
    for (const val of Object.values(values)) {
        if (!isValueEmpty(val)) count++;
    }
    return count;
}

export function useListFilters(
    configs: FilterConfig[],
    options: UseListFiltersOptions = {}
): UseListFiltersReturn {
    const { syncToUrl = true, onPageReset } = options;

    const [searchParams, setSearchParams] = syncToUrl
        ? useSearchParams()
        : [null, null];

    // Initialize from URL params when syncToUrl is true
    const [filterValues, setFilterValues] = useState<FilterValues>(() => {
        if (syncToUrl && searchParams) {
            return deserializeFilters(searchParams, configs);
        }
        return {};
    });

    // Track pending debounced values
    const debounceTimers = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());
    const onPageResetRef = useRef(onPageReset);
    onPageResetRef.current = onPageReset;

    // Sync filter values to URL when they change
    useEffect(() => {
        if (!syncToUrl || !setSearchParams) return;
        const serialized = serializeFilters(filterValues);
        setSearchParams(serialized, { replace: true });
    }, [filterValues, syncToUrl, setSearchParams]);

    // Cleanup debounce timers on unmount
    useEffect(() => {
        return () => {
            debounceTimers.current.forEach((timer) => clearTimeout(timer));
        };
    }, []);

    const updateFilter = useCallback(
        (key: string, value: string | string[] | undefined) => {
            const config = configs.find((c) => c.key === key);
            const shouldDebounce = config && DEBOUNCED_TYPES.has(config.type);

            // Clear any existing debounce timer for this key
            const existing = debounceTimers.current.get(key);
            if (existing) {
                clearTimeout(existing);
                debounceTimers.current.delete(key);
            }

            const applyUpdate = () => {
                setFilterValues((prev) => {
                    const next = { ...prev };
                    if (isValueEmpty(value)) {
                        delete next[key];
                    } else {
                        next[key] = value;
                    }
                    return next;
                });
                onPageResetRef.current?.();
            };

            if (shouldDebounce) {
                const timer = setTimeout(applyUpdate, DEBOUNCE_MS);
                debounceTimers.current.set(key, timer);
            } else {
                applyUpdate();
            }
        },
        [configs]
    );

    const clearFilters = useCallback(() => {
        // Clear all pending debounce timers
        debounceTimers.current.forEach((timer) => clearTimeout(timer));
        debounceTimers.current.clear();

        setFilterValues({});
        onPageResetRef.current?.();
    }, []);

    const activeFilterCount = countActiveFilters(filterValues);

    return {
        filterValues,
        updateFilter,
        clearFilters,
        hasActiveFilters: activeFilterCount > 0,
        activeFilterCount,
    };
}
