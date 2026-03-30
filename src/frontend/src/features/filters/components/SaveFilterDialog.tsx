import { useState } from 'react';
import { Modal } from '@/components/common/Modal';
import { FormField } from '@/components/forms/FormField';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { Save } from 'lucide-react';

interface SaveFilterDialogProps {
    filters: object;
    onSaved?: () => void;
}

export function SaveFilterDialog({ filters, onSaved }: SaveFilterDialogProps) {
    const { addToast } = useToast();
    const [open, setOpen] = useState(false);
    const [name, setName] = useState('');
    const [saving, setSaving] = useState(false);

    const hasFilters = Object.values(filters).some((v) => v !== undefined && v !== '' && v !== null);

    if (!hasFilters) return null;

    const handleSave = async () => {
        if (!name.trim()) return;
        setSaving(true);
        try {
            await workApi.createSavedFilter({ name: name.trim(), filters: JSON.stringify(filters) });
            addToast('success', 'Filter saved');
            setOpen(false);
            setName('');
            onSaved?.();
        } catch {
            addToast('error', 'Failed to save filter');
        } finally {
            setSaving(false);
        }
    };

    return (
        <>
            <button
                onClick={() => setOpen(true)}
                className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-1.5 text-sm font-medium text-foreground hover:bg-accent"
            >
                <Save size={14} /> Save Filter
            </button>

            <Modal open={open} onClose={() => setOpen(false)} title="Save Filter">
                <div className="space-y-4">
                    <FormField name="filterName" label="Filter Name" required>
                        <input
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            placeholder="My filter"
                            className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                        />
                    </FormField>
                    <div className="flex justify-end gap-2">
                        <button onClick={() => setOpen(false)} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">Cancel</button>
                        <button onClick={handleSave} disabled={!name.trim() || saving} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                            {saving ? 'Saving...' : 'Save'}
                        </button>
                    </div>
                </div>
            </Modal>
        </>
    );
}
