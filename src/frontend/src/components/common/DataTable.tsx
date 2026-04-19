import { ArrowUp, ArrowDown, ArrowUpDown } from 'lucide-react';
import { SkeletonLoader } from './SkeletonLoader.js';

export interface Column<T> {
    key: string;
    header: string;
    sortable?: boolean;
    render?: (row: T) => React.ReactNode;
}

interface DataTableProps<T> {
    columns: Column<T>[];
    data: T[];
    loading?: boolean;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
    onSort?: (key: string) => void;
    onRowClick?: (row: T) => void;
    keyExtractor: (row: T) => string;
    emptyMessage?: string;
}

export function DataTable<T>({
    columns,
    data,
    loading = false,
    sortBy,
    sortDirection,
    onSort,
    onRowClick,
    keyExtractor,
    emptyMessage,
}: DataTableProps<T>) {
    if (loading) {
        return <SkeletonLoader variant="table" rows={5} columns={columns.length} />;
    }

    const getSortIcon = (key: string) => {
        if (sortBy !== key) return <ArrowUpDown size={14} className="text-muted-foreground/50" />;
        return sortDirection === 'asc' ? (
            <ArrowUp size={14} className="text-foreground" />
        ) : (
            <ArrowDown size={14} className="text-foreground" />
        );
    };

    return (
        <div className="overflow-x-auto rounded-md border border-border">
            <table className="w-full text-sm">
                <thead>
                    <tr className="border-b border-border bg-muted/50">
                        {columns.map((col) => (
                            <th
                                key={col.key}
                                className={`px-4 py-3 text-left font-medium text-muted-foreground ${col.sortable && onSort ? 'cursor-pointer select-none' : ''
                                    }`}
                                onClick={col.sortable && onSort ? () => onSort(col.key) : undefined}
                            >
                                <span className="flex items-center gap-1">
                                    {col.header}
                                    {col.sortable && onSort && getSortIcon(col.key)}
                                </span>
                            </th>
                        ))}
                    </tr>
                </thead>
                <tbody>
                    {data.length === 0 ? (
                        <tr>
                            <td colSpan={columns.length} className="px-4 py-8 text-center text-muted-foreground">
                                {emptyMessage ?? 'No data available'}
                            </td>
                        </tr>
                    ) : (
                        data.map((row) => (
                            <tr
                                key={keyExtractor(row)}
                                onClick={onRowClick ? () => onRowClick(row) : undefined}
                                className={`border-b border-border last:border-0 ${onRowClick ? 'cursor-pointer hover:bg-muted/50' : ''
                                    }`}
                            >
                                {columns.map((col) => (
                                    <td key={col.key} className="px-4 py-3 text-foreground">
                                        {col.render
                                            ? col.render(row)
                                            : String((row as Record<string, unknown>)[col.key] ?? '')}
                                    </td>
                                ))}
                            </tr>
                        ))
                    )}
                </tbody>
            </table>
        </div>
    );
}
