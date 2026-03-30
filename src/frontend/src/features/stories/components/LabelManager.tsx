import { useState, useEffect } from 'react';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { Modal } from '@/components/common/Modal';
import { ColorPicker } from '@/components/forms/ColorPicker';
import { FormField } from '@/components/forms/FormField';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { useAuth } from '@/hooks/useAuth';
import type { Label } from '@/types/work';
import { Plus, X, Settings, Pencil, Trash2, Search } from 'lucide-react';

interface LabelManagerProps {
    storyId: string;
    appliedLabels: Label[];
    onLabelsChanged: () => void;
}

export function LabelManager({ storyId, appliedLabels, onLabelsChanged }: LabelManagerProps) {
    const { addToast } = useToast();
    const { user } = useAuth();
    const [allLabels, setAllLabels] = useState<Label[]>([]);
    const [dropdownOpen, setDropdownOpen] = useState(false);
    const [searchQuery, setSearchQuery] = useState('');
    const [manageOpen, setManageOpen] = useState(false);
    const [editingLabel, setEditingLabel] = useState<Label | null>(null);
    const [newLabelName, setNewLabelName] = useState('');
    const [newLabelColor, setNewLabelColor] = useState('#3b82f6');
    const [saving, setSaving] = useState(false);
    const [manageError, setManageError] = useState('');

    const canManage = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead';
    const appliedIds = new Set(appliedLabels.map((l) => l.labelId));

    const fetchLabels = () => {
        workApi.getLabels().then(setAllLabels).catch(() => { });
    };

    useEffect(() => { fetchLabels(); }, []);

    const availableLabels = allLabels.filter(
        (l) => !appliedIds.has(l.labelId) && l.name.toLowerCase().includes(searchQuery.toLowerCase())
    );

    const handleApply = async (labelId: string) => {
        try {
            await workApi.applyLabel(storyId, { labelId });
            addToast('success', 'Label added');
            setDropdownOpen(false);
            setSearchQuery('');
            onLabelsChanged();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to add label');
        }
    };

    const handleRemove = async (labelId: string) => {
        try {
            await workApi.removeLabel(storyId, labelId);
            addToast('success', 'Label removed');
            onLabelsChanged();
        } catch {
            addToast('error', 'Failed to remove label');
        }
    };

    const handleCreateLabel = async () => {
        if (!newLabelName.trim()) return;
        setSaving(true);
        setManageError('');
        try {
            await workApi.createLabel({ name: newLabelName.trim(), color: newLabelColor });
            addToast('success', 'Label created');
            setNewLabelName('');
            setNewLabelColor('#3b82f6');
            fetchLabels();
        } catch (err) {
            if (err instanceof ApiError) setManageError(mapErrorCode(err.errorCode));
            else setManageError('Failed to create label');
        } finally {
            setSaving(false);
        }
    };

    const handleUpdateLabel = async () => {
        if (!editingLabel || !newLabelName.trim()) return;
        setSaving(true);
        setManageError('');
        try {
            await workApi.updateLabel(editingLabel.labelId, { name: newLabelName.trim(), color: newLabelColor });
            addToast('success', 'Label updated');
            setEditingLabel(null);
            setNewLabelName('');
            setNewLabelColor('#3b82f6');
            fetchLabels();
            onLabelsChanged();
        } catch (err) {
            if (err instanceof ApiError) setManageError(mapErrorCode(err.errorCode));
            else setManageError('Failed to update label');
        } finally {
            setSaving(false);
        }
    };

    const handleDeleteLabel = async (labelId: string) => {
        try {
            await workApi.deleteLabel(labelId);
            addToast('success', 'Label deleted');
            fetchLabels();
            onLabelsChanged();
        } catch {
            addToast('error', 'Failed to delete label');
        }
    };

    return (
        <div className="space-y-2">
            <div className="flex flex-wrap gap-1.5">
                {appliedLabels.map((l) => (
                    <span
                        key={l.labelId}
                        className="inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium text-white"
                        style={{ backgroundColor: l.color }}
                    >
                        {l.name}
                        <button
                            onClick={() => handleRemove(l.labelId)}
                            className="ml-0.5 rounded-full p-0.5 hover:bg-white/20"
                            aria-label={`Remove ${l.name}`}
                        >
                            <X size={10} />
                        </button>
                    </span>
                ))}
            </div>

            <div className="flex items-center gap-2">
                <div className="relative">
                    <button
                        onClick={() => { setDropdownOpen((v) => !v); setSearchQuery(''); }}
                        className="inline-flex items-center gap-1 rounded-md border border-input px-2 py-1 text-xs font-medium text-foreground hover:bg-accent"
                    >
                        <Plus size={12} /> Add Label
                    </button>
                    {dropdownOpen && (
                        <div className="absolute left-0 z-50 mt-1 w-56 rounded-md border border-border bg-popover p-2 shadow-lg">
                            <div className="relative mb-2">
                                <Search size={12} className="absolute left-2 top-1/2 -translate-y-1/2 text-muted-foreground" />
                                <input
                                    type="text"
                                    value={searchQuery}
                                    onChange={(e) => setSearchQuery(e.target.value)}
                                    placeholder="Search labels..."
                                    className="h-7 w-full rounded border border-input bg-background pl-7 pr-2 text-xs text-foreground"
                                    autoFocus
                                />
                            </div>
                            <ul className="max-h-40 overflow-auto">
                                {availableLabels.length === 0 && (
                                    <li className="px-2 py-1 text-xs text-muted-foreground">No labels found</li>
                                )}
                                {availableLabels.map((l) => (
                                    <li key={l.labelId}>
                                        <button
                                            type="button"
                                            onClick={() => handleApply(l.labelId)}
                                            className="flex w-full items-center gap-2 rounded px-2 py-1 text-xs hover:bg-accent text-popover-foreground"
                                        >
                                            <span className="h-3 w-3 rounded-full" style={{ backgroundColor: l.color }} />
                                            {l.name}
                                        </button>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}
                </div>

                {canManage && (
                    <button
                        onClick={() => { setManageOpen(true); setManageError(''); setEditingLabel(null); setNewLabelName(''); setNewLabelColor('#3b82f6'); }}
                        className="inline-flex items-center gap-1 rounded-md border border-input px-2 py-1 text-xs font-medium text-foreground hover:bg-accent"
                    >
                        <Settings size={12} /> Manage Labels
                    </button>
                )}
            </div>

            <Modal open={manageOpen} onClose={() => setManageOpen(false)} title="Manage Labels">
                <div className="space-y-4">
                    {manageError && <p className="text-sm text-destructive">{manageError}</p>}

                    <div className="space-y-3">
                        <FormField name="labelName" label={editingLabel ? 'Edit Label' : 'New Label'} required>
                            <input
                                value={newLabelName}
                                onChange={(e) => setNewLabelName(e.target.value)}
                                placeholder="Label name"
                                className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground"
                            />
                        </FormField>
                        <ColorPicker value={newLabelColor} onChange={setNewLabelColor} />
                        <div className="flex gap-2">
                            <button
                                onClick={editingLabel ? handleUpdateLabel : handleCreateLabel}
                                disabled={saving || !newLabelName.trim()}
                                className="rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                            >
                                {saving ? 'Saving...' : editingLabel ? 'Update' : 'Create'}
                            </button>
                            {editingLabel && (
                                <button
                                    onClick={() => { setEditingLabel(null); setNewLabelName(''); setNewLabelColor('#3b82f6'); }}
                                    className="rounded-md border border-input px-3 py-1.5 text-sm text-foreground hover:bg-accent"
                                >
                                    Cancel
                                </button>
                            )}
                        </div>
                    </div>

                    <div className="border-t border-border pt-3">
                        <p className="mb-2 text-sm font-medium text-foreground">Existing Labels</p>
                        <ul className="space-y-1 max-h-48 overflow-auto">
                            {allLabels.map((l) => (
                                <li key={l.labelId} className="flex items-center justify-between rounded px-2 py-1.5 hover:bg-accent">
                                    <span className="flex items-center gap-2 text-sm text-foreground">
                                        <span className="h-3 w-3 rounded-full" style={{ backgroundColor: l.color }} />
                                        {l.name}
                                    </span>
                                    <div className="flex gap-1">
                                        <button
                                            onClick={() => { setEditingLabel(l); setNewLabelName(l.name); setNewLabelColor(l.color); setManageError(''); }}
                                            className="rounded p-1 text-muted-foreground hover:text-foreground"
                                            aria-label={`Edit ${l.name}`}
                                        >
                                            <Pencil size={12} />
                                        </button>
                                        <button
                                            onClick={() => handleDeleteLabel(l.labelId)}
                                            className="rounded p-1 text-muted-foreground hover:text-destructive"
                                            aria-label={`Delete ${l.name}`}
                                        >
                                            <Trash2 size={12} />
                                        </button>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    </div>
                </div>
            </Modal>
        </div>
    );
}
