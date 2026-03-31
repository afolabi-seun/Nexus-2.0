import { useState } from 'react';
import { Filter, X } from 'lucide-react';
import type { FilterConfig, FilterValues } from '@/types/filters';
import { FilterField } from './FilterField';
import { SavedFilterDropdown } from '@/features/filters/components/SavedFilterDropdown';
import { SaveFilterDialog } from '@/features/filters/components/SaveFilterDialog';

interface ListFilterProps {
    configs: FilterConfig[];
    values: FilterValues;
    onUpdateFilter: (key: string, value: string | string[] | undefined) => void;
    onClearFilters: () => void;
    hasActiveFilters: boolean;
    activeFilterCount: number;
    enableSavedFilters?: boolean;
    onApplySavedFilter?: (filters: Record<string, unknown>) => void;
}

export function ListFilter({
    configs,
    values,
    onUpdateFilter,
    onClearFilters,
    hasActiveFilters,
    activeFilterCount,
    enableSavedFilters = false,
    onApplySavedFilter,
}: ListFilterProps) {
    const [open, setOpen] = useState(false);

    const handleApplySaved = (filters: Record<string, unknown>) => {
        if (onApplySavedFilter) {
            onApplySavedFilter(filters);
        } else {
            // Default: apply each key via onUpdateFilter
            for (const config of configs) {
                const val = filters[config.key];
                onUpdateFilter(config.key, val as string | string[] | undefined);
            }
        }
    };

    return (
        <div className="space-y-3">
            {/* Toolbar row */}
            <div className="flex items-center gap-2">
                <button
                    type="button"
                    onClick={() => setOpen((prev) => !prev)}
                    aria-expanded={open}
                    className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-1.5 text-sm font-medium text-foreground hover:bg-accent"
                >
                    <Filter size={14} />
                    Filters
                    {activeFilterCount > 0 && (
                        <span className="ml-1 inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-xs font-semibold text-primary-foreground">
                            {activeFilterCount}
                        </span>
                    )}
                </button>

                {enableSavedFilters && (
                    <>
                        <SavedFilterDropdown onApply={handleApplySaved} />
                        <SaveFilterDialog filters={values} />
                    </>
                )}

                {hasActiveFilters && open && (
                    <button
                        type="button"
                        onClick={onClearFilters}
                        className="inline-flex items-center gap-1 rounded-md px-2 py-1.5 text-sm text-muted-foreground hover:text-foreground"
                    >
                        <X size={14} />
                        Clear all
                    </button>
                )}
            </div>

            {/* Collapsible filter region */}
            {open && (
                <div
                    aria-label="Filter options"
                    className="rounded-md border border-border bg-card p-4"
                >
                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
                        {configs.map((config) => (
                            <FilterField
                                key={config.key}
                                config={config}
                                value={values[config.key]}
                                onChange={(val) => onUpdateFilter(config.key, val)}
                            />
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}
