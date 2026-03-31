export type FilterFieldType =
    | 'select'
    | 'multi-select'
    | 'text-search'
    | 'date'
    | 'date-range'
    | 'async-search';

export interface FilterOption {
    value: string;
    label: string;
}

export interface FilterConfig {
    key: string;
    label: string;
    type: FilterFieldType;
    options?: FilterOption[];
    loadOptions?: (query: string) => Promise<FilterOption[]>;
    placeholder?: string;
}

export type FilterValues = Record<string, string | string[] | undefined>;
