import { useState, useEffect, useRef } from 'react';
import { workApi } from '@/api/workApi';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { useToast } from '@/components/common/Toast';
import type { SavedFilter } from '@/types/work';
import { Filter, Trash2, ChevronDown } from 'lucide-react';

interface SavedFilterDropdownProps {
    onApply: (filters: Record<string, unknown>) => void;
}

export function SavedFilterDropdown({ onApply }: SavedFilterDropdownProps) {
    const { addToast } = useToast();
    const [filters, setFilters] = useState<SavedFilter[]>([]);
    const [open, setOpen] = useState(false);
    const [deleteTarget, setDeleteTarget] = useState<string | null>(null);
    const containerRef = useRef<HTMLDivElement>(null);

    const fetchFilters = () => {
        workApi.getSavedFilters().then(setFilters).catch(() => setFilters([]));
    };

    useEffect(() => { fetchFilters(); }, []);

    useEffect(() => {
        function handleClickOutside(e: MouseEvent) {
            if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
                setOpen(false);
            }
        }
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const handleApply = (filter: SavedFilter) => {
        try {
            const parsed = JSON.parse(filter.filters);
            onApply(parsed);
            setOpen(false);
        } catch {
            addToast('error', 'Invalid filter data');
        }
    };

    const handleDelete = async () => {
        if (!deleteTarget) return;
        try {
            await workApi.deleteSavedFilter(deleteTarget);
            addToast('success', 'Filter deleted');
            setDeleteTarget(null);
            fetchFilters();
        } catch {
            addToast('error', 'Failed to delete filter');
        }
    };

    if (filters.length === 0) return null;

    return (
        <div className="relative" ref={containerRef}>
            <button
                onClick={() => setOpen(!open)}
                className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-1.5 text-sm font-medium text-foreground hover:bg-accent"
            >
                <Filter size={14} /> Saved Filters <ChevronDown size={12} />
            </button>

            {open && (
                <div className="absolute right-0 z-50 mt-1 w-64 rounded-md border border-border bg-popover py-1 shadow-lg">
                    {filters.map((f) => (
                        <div key={f.savedFilterId} className="flex items-center justify-between px-3 py-2 hover:bg-accent">
                            <button
                                onClick={() => handleApply(f)}
                                className="flex-1 text-left text-sm text-popover-foreground truncate"
                            >
                                {f.name}
                            </button>
                            <button
                                onClick={(e) => { e.stopPropagation(); setDeleteTarget(f.savedFilterId); }}
                                className="ml-2 text-destructive hover:text-destructive/80"
                            >
                                <Trash2 size={12} />
                            </button>
                        </div>
                    ))}
                </div>
            )}

            <ConfirmDialog
                open={deleteTarget !== null}
                onConfirm={handleDelete}
                onCancel={() => setDeleteTarget(null)}
                title="Delete Saved Filter"
                message="Are you sure you want to delete this saved filter?"
                confirmLabel="Delete"
            />
        </div>
    );
}
