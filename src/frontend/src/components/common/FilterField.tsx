import { useState, useEffect, useRef, useCallback } from 'react';
import { Search, Loader2 } from 'lucide-react';
import type { FilterConfig, FilterOption } from '@/types/filters';

interface FilterFieldProps {
    config: FilterConfig;
    value: string | string[] | undefined;
    onChange: (value: string | string[] | undefined) => void;
}

const inputClass =
    'h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring';

export function FilterField({ config, value, onChange }: FilterFieldProps) {
    const fieldId = `filter-${config.key}`;

    return (
        <div>
            <label htmlFor={fieldId} className="mb-1 block text-sm font-medium text-foreground">
                {config.label}
            </label>
            {renderField(config, value, onChange, fieldId)}
        </div>
    );
}

function renderField(
    config: FilterConfig,
    value: string | string[] | undefined,
    onChange: (value: string | string[] | undefined) => void,
    fieldId: string
) {
    switch (config.type) {
        case 'select':
            return <SelectField config={config} value={value as string | undefined} onChange={onChange} fieldId={fieldId} />;
        case 'multi-select':
            return <MultiSelectField config={config} value={value as string[] | undefined} onChange={onChange} fieldId={fieldId} />;
        case 'text-search':
            return <TextSearchField config={config} value={value as string | undefined} onChange={onChange} fieldId={fieldId} />;
        case 'date':
            return <DateField value={value as string | undefined} onChange={onChange} fieldId={fieldId} />;
        case 'date-range':
            return <DateRangeField config={config} value={value as string | undefined} onChange={onChange} fieldId={fieldId} />;
        case 'async-search':
            return <AsyncSearchField config={config} value={value as string | undefined} onChange={onChange} fieldId={fieldId} />;
        default:
            return null;
    }
}


/* ── Select ─────────────────────────────────────────────────────────── */

function SelectField({
    config,
    value,
    onChange,
    fieldId,
}: {
    config: FilterConfig;
    value: string | undefined;
    onChange: (v: string | undefined) => void;
    fieldId: string;
}) {
    const [asyncOptions, setAsyncOptions] = useState<FilterOption[] | null>(null);

    useEffect(() => {
        if (!config.options && config.loadOptions) {
            config.loadOptions('').then(setAsyncOptions).catch(() => setAsyncOptions([]));
        }
    }, [config]);

    const options = config.options ?? asyncOptions ?? [];

    return (
        <select
            id={fieldId}
            value={value ?? ''}
            onChange={(e) => onChange(e.target.value || undefined)}
            className={inputClass}
        >
            <option value="">All</option>
            {options.map((opt) => (
                <option key={opt.value} value={opt.value}>
                    {opt.label}
                </option>
            ))}
        </select>
    );
}

/* ── Multi-select (toggle pills) ────────────────────────────────────── */

function MultiSelectField({
    config,
    value,
    onChange,
    fieldId,
}: {
    config: FilterConfig;
    value: string[] | undefined;
    onChange: (v: string[] | undefined) => void;
    fieldId: string;
}) {
    const selected = value ?? [];
    const options = config.options ?? [];

    const toggle = (optValue: string) => {
        const next = selected.includes(optValue)
            ? selected.filter((v) => v !== optValue)
            : [...selected, optValue];
        onChange(next.length > 0 ? next : undefined);
    };

    return (
        <div id={fieldId} role="group" aria-label={config.label} className="flex flex-wrap gap-1.5">
            {options.map((opt) => {
                const isActive = selected.includes(opt.value);
                return (
                    <button
                        key={opt.value}
                        type="button"
                        aria-pressed={isActive}
                        onClick={() => toggle(opt.value)}
                        className={`rounded-full border px-2.5 py-1 text-xs font-medium transition-colors ${isActive
                                ? 'border-primary bg-primary text-primary-foreground'
                                : 'border-input bg-background text-foreground hover:bg-accent'
                            }`}
                    >
                        {opt.label}
                    </button>
                );
            })}
        </div>
    );
}

/* ── Text search ────────────────────────────────────────────────────── */

function TextSearchField({
    config,
    value,
    onChange,
    fieldId,
}: {
    config: FilterConfig;
    value: string | undefined;
    onChange: (v: string | undefined) => void;
    fieldId: string;
}) {
    return (
        <div className="relative">
            <Search size={14} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
            <input
                id={fieldId}
                type="text"
                value={value ?? ''}
                onChange={(e) => onChange(e.target.value || undefined)}
                placeholder={config.placeholder ?? 'Search…'}
                className={`${inputClass} pl-8`}
            />
        </div>
    );
}

/* ── Date ────────────────────────────────────────────────────────────── */

function DateField({
    value,
    onChange,
    fieldId,
}: {
    value: string | undefined;
    onChange: (v: string | undefined) => void;
    fieldId: string;
}) {
    return (
        <input
            id={fieldId}
            type="date"
            value={value ?? ''}
            onChange={(e) => onChange(e.target.value || undefined)}
            className={inputClass}
        />
    );
}

/* ── Date range ─────────────────────────────────────────────────────── */

function DateRangeField({
    config,
    value,
    onChange,
    fieldId,
}: {
    config: FilterConfig;
    value: string | undefined;
    onChange: (v: string | undefined) => void;
    fieldId: string;
}) {
    // Stored as "from|to" string
    const [from, to] = (value ?? '|').split('|');

    const update = (newFrom: string, newTo: string) => {
        if (!newFrom && !newTo) {
            onChange(undefined);
        } else {
            onChange(`${newFrom}|${newTo}`);
        }
    };

    return (
        <div className="flex items-center gap-2">
            <input
                id={fieldId}
                type="date"
                value={from ?? ''}
                onChange={(e) => update(e.target.value, to ?? '')}
                aria-label={`${config.label} from`}
                className={inputClass}
            />
            <span className="text-xs text-muted-foreground">to</span>
            <input
                id={`${fieldId}-to`}
                type="date"
                value={to ?? ''}
                onChange={(e) => update(from ?? '', e.target.value)}
                aria-label={`${config.label} to`}
                className={inputClass}
            />
        </div>
    );
}

/* ── Async search ───────────────────────────────────────────────────── */

function AsyncSearchField({
    config,
    value,
    onChange,
    fieldId,
}: {
    config: FilterConfig;
    value: string | undefined;
    onChange: (v: string | undefined) => void;
    fieldId: string;
}) {
    const [query, setQuery] = useState(value ?? '');
    const [options, setOptions] = useState<FilterOption[]>([]);
    const [loading, setLoading] = useState(false);
    const [open, setOpen] = useState(false);
    const containerRef = useRef<HTMLDivElement>(null);
    const debounceRef = useRef<ReturnType<typeof setTimeout>>();

    // Sync external value changes
    useEffect(() => {
        setQuery(value ?? '');
    }, [value]);

    const fetchOptions = useCallback(
        (q: string) => {
            if (!config.loadOptions || q.length < 2) {
                setOptions([]);
                setOpen(false);
                return;
            }
            setLoading(true);
            config
                .loadOptions(q)
                .then((result) => {
                    setOptions(result);
                    setOpen(result.length > 0);
                })
                .catch(() => {
                    setOptions([]);
                    setOpen(false);
                })
                .finally(() => setLoading(false));
        },
        [config]
    );

    const handleInput = (q: string) => {
        setQuery(q);
        if (debounceRef.current) clearTimeout(debounceRef.current);
        if (q.length < 2) {
            setOptions([]);
            setOpen(false);
            if (!q) onChange(undefined);
            return;
        }
        debounceRef.current = setTimeout(() => fetchOptions(q), 300);
    };

    const selectOption = (opt: FilterOption) => {
        setQuery(opt.label);
        onChange(opt.value);
        setOpen(false);
    };

    // Close dropdown on outside click
    useEffect(() => {
        const handler = (e: MouseEvent) => {
            if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
                setOpen(false);
            }
        };
        document.addEventListener('mousedown', handler);
        return () => document.removeEventListener('mousedown', handler);
    }, []);

    useEffect(() => {
        return () => {
            if (debounceRef.current) clearTimeout(debounceRef.current);
        };
    }, []);

    return (
        <div className="relative" ref={containerRef}>
            <Search size={14} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
            {loading && (
                <Loader2 size={14} className="absolute right-3 top-1/2 -translate-y-1/2 animate-spin text-muted-foreground" />
            )}
            <input
                id={fieldId}
                type="text"
                value={query}
                onChange={(e) => handleInput(e.target.value)}
                placeholder={config.placeholder ?? 'Search…'}
                className={`${inputClass} pl-8`}
            />
            {open && options.length > 0 && (
                <ul className="absolute z-50 mt-1 max-h-48 w-full overflow-auto rounded-md border border-border bg-popover py-1 shadow-lg">
                    {options.map((opt) => (
                        <li key={opt.value}>
                            <button
                                type="button"
                                onClick={() => selectOption(opt)}
                                className="w-full px-3 py-1.5 text-left text-sm text-popover-foreground hover:bg-accent"
                            >
                                {opt.label}
                            </button>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
