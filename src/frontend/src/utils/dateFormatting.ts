import { format, formatDistanceToNow, parseISO, isValid } from 'date-fns';

export function formatDate(
    dateStr: string | null | undefined,
    pattern = 'MMM d, yyyy'
): string {
    if (!dateStr) return '—';
    const date = parseISO(dateStr);
    if (!isValid(date)) return '—';
    return format(date, pattern);
}

export function formatDateTime(
    dateStr: string | null | undefined,
    pattern = 'MMM d, yyyy HH:mm'
): string {
    if (!dateStr) return '—';
    const date = parseISO(dateStr);
    if (!isValid(date)) return '—';
    return format(date, pattern);
}

export function formatRelative(
    dateStr: string | null | undefined
): string {
    if (!dateStr) return '—';
    const date = parseISO(dateStr);
    if (!isValid(date)) return '—';
    return formatDistanceToNow(date, { addSuffix: true });
}

export function formatDateByPreference(
    dateStr: string | null | undefined,
    dateFormat: 'ISO' | 'US' | 'EU' = 'ISO'
): string {
    if (!dateStr) return '—';
    const date = parseISO(dateStr);
    if (!isValid(date)) return '—';

    switch (dateFormat) {
        case 'US':
            return format(date, 'MM/dd/yyyy');
        case 'EU':
            return format(date, 'dd/MM/yyyy');
        case 'ISO':
        default:
            return format(date, 'yyyy-MM-dd');
    }
}
