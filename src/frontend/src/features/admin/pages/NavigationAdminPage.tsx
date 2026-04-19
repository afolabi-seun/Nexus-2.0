import { useState, useEffect, useCallback } from 'react';
import { profileApi } from '@/api/profileApi';
import { Modal } from '@/components/common/Modal';
import { FormField } from '@/components/forms/FormField';
import { useToast } from '@/components/common/Toast';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { NavigationItem } from '@/types/profile';
import { Plus, Pencil, Trash2, Eye, EyeOff, GripVertical } from 'lucide-react';

const SECTIONS = ['Work', 'Tracking', 'Team', 'Organization'];
const ICONS = [
    'LayoutDashboard', 'FolderKanban', 'BookOpen', 'Columns3', 'Timer', 'Users',
    'Building2', 'BarChart3', 'Settings', 'Mail', 'Kanban', 'CalendarDays',
    'Archive', 'CreditCard', 'Clock', 'TrendingUp', 'Bell', 'ClipboardList',
];

interface FormState {
    label: string;
    path: string;
    icon: string;
    section: string;
    sortOrder: number;
    parentId: string;
    minPermissionLevel: number;
    isEnabled: boolean;
}

const emptyForm: FormState = {
    label: '', path: '', icon: 'LayoutDashboard', section: 'Work',
    sortOrder: 1, parentId: '', minPermissionLevel: 25, isEnabled: true,
};

export function NavigationAdminPage() {
    const { addToast } = useToast();
    const [items, setItems] = useState<NavigationItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [modalOpen, setModalOpen] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [form, setForm] = useState<FormState>(emptyForm);
    const [saving, setSaving] = useState(false);
    const [deleteTarget, setDeleteTarget] = useState<string | null>(null);

    const fetchItems = useCallback(async () => {
        setLoading(true);
        try {
            const data = await profileApi.getAllNavigation();
            setItems(data);
        } catch {
            addToast('error', 'Failed to load navigation items');
        } finally {
            setLoading(false);
        }
    }, [addToast]);

    useEffect(() => { fetchItems(); }, [fetchItems]);

    const openCreate = () => {
        setEditingId(null);
        setForm(emptyForm);
        setModalOpen(true);
    };

    const openEdit = (item: NavigationItem) => {
        setEditingId(item.navigationItemId);
        setForm({
            label: item.label,
            path: item.path,
            icon: item.icon,
            section: item.section || 'Work',
            sortOrder: item.sortOrder,
            parentId: item.parentId ?? '',
            minPermissionLevel: item.minPermissionLevel,
            isEnabled: item.isEnabled,
        });
        setModalOpen(true);
    };

    const handleSave = async () => {
        setSaving(true);
        try {
            if (editingId) {
                await profileApi.updateNavigationItem(editingId, {
                    label: form.label, path: form.path, icon: form.icon,
                    sortOrder: form.sortOrder, minPermissionLevel: form.minPermissionLevel,
                    isEnabled: form.isEnabled,
                });
                addToast('success', 'Navigation item updated');
            } else {
                await profileApi.createNavigationItem({
                    label: form.label, path: form.path, icon: form.icon,
                    section: form.section, sortOrder: form.sortOrder,
                    parentId: form.parentId || null, minPermissionLevel: form.minPermissionLevel,
                    isEnabled: form.isEnabled,
                });
                addToast('success', 'Navigation item created');
            }
            setModalOpen(false);
            fetchItems();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to save');
        } finally {
            setSaving(false);
        }
    };

    const handleDelete = async () => {
        if (!deleteTarget) return;
        try {
            await profileApi.deleteNavigationItem(deleteTarget);
            addToast('success', 'Navigation item deleted');
            setDeleteTarget(null);
            fetchItems();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to delete');
        }
    };

    const allItems = items.flatMap((item) => [item, ...(item.children ?? [])]);
    const grouped = SECTIONS.map((section) => ({
        section,
        items: allItems.filter((i) => i.section === section).sort((a, b) => a.sortOrder - b.sortOrder),
    })).filter((g) => g.items.length > 0);

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-semibold text-foreground">Navigation Management</h1>
                <button onClick={openCreate} className="inline-flex items-center gap-1.5 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90">
                    <Plus size={16} /> Add Item
                </button>
            </div>

            {loading && <p className="text-sm text-muted-foreground">Loading…</p>}

            {!loading && grouped.map(({ section, items: sectionItems }) => (
                <section key={section} className="space-y-2">
                    <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">{section}</h2>
                    <div className="rounded-md border border-border overflow-hidden">
                        <table className="w-full text-sm">
                            <thead>
                                <tr className="border-b border-border bg-muted/50">
                                    <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground w-8"></th>
                                    <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Label</th>
                                    <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Path</th>
                                    <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Icon</th>
                                    <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Order</th>
                                    <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Min Level</th>
                                    <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Status</th>
                                    <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {sectionItems.map((item) => (
                                    <tr key={item.navigationItemId} className="border-b border-border last:border-0">
                                        <td className="px-3 py-2 text-muted-foreground"><GripVertical size={14} /></td>
                                        <td className="px-3 py-2 font-medium text-foreground">
                                            {item.parentId && <span className="text-muted-foreground mr-1">└</span>}
                                            {item.label}
                                        </td>
                                        <td className="px-3 py-2 text-muted-foreground font-mono text-xs">{item.path}</td>
                                        <td className="px-3 py-2 text-muted-foreground text-xs">{item.icon}</td>
                                        <td className="px-3 py-2 text-muted-foreground">{item.sortOrder}</td>
                                        <td className="px-3 py-2 text-muted-foreground">{item.minPermissionLevel}</td>
                                        <td className="px-3 py-2">
                                            {item.isEnabled
                                                ? <span className="inline-flex items-center gap-1 text-xs text-green-600"><Eye size={12} /> Active</span>
                                                : <span className="inline-flex items-center gap-1 text-xs text-muted-foreground"><EyeOff size={12} /> Disabled</span>
                                            }
                                        </td>
                                        <td className="px-3 py-2 text-right">
                                            <div className="flex items-center justify-end gap-1">
                                                <button onClick={() => openEdit(item)} className="rounded p-1 text-muted-foreground hover:text-foreground hover:bg-accent" title="Edit">
                                                    <Pencil size={14} />
                                                </button>
                                                <button onClick={() => setDeleteTarget(item.navigationItemId)} className="rounded p-1 text-muted-foreground hover:text-destructive hover:bg-accent" title="Delete">
                                                    <Trash2 size={14} />
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </section>
            ))}

            {!loading && allItems.length === 0 && (
                <p className="py-8 text-center text-sm text-muted-foreground">No navigation items. Click "Add Item" to create one.</p>
            )}

            {/* Create/Edit Modal */}
            <Modal open={modalOpen} onClose={() => setModalOpen(false)} title={editingId ? 'Edit Navigation Item' : 'Create Navigation Item'}>
                <div className="space-y-4">
                    <FormField name="label" label="Label" required>
                        <input value={form.label} onChange={(e) => setForm((f) => ({ ...f, label: e.target.value }))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                    </FormField>
                    <FormField name="path" label="Path" required>
                        <input value={form.path} onChange={(e) => setForm((f) => ({ ...f, path: e.target.value }))} placeholder="/projects" className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                    </FormField>
                    <div className="grid grid-cols-2 gap-4">
                        <FormField name="icon" label="Icon">
                            <select value={form.icon} onChange={(e) => setForm((f) => ({ ...f, icon: e.target.value }))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                                {ICONS.map((icon) => <option key={icon} value={icon}>{icon}</option>)}
                            </select>
                        </FormField>
                        <FormField name="section" label="Section">
                            <select value={form.section} onChange={(e) => setForm((f) => ({ ...f, section: e.target.value }))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                                {SECTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
                            </select>
                        </FormField>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                        <FormField name="sortOrder" label="Sort Order">
                            <input type="number" min={1} value={form.sortOrder} onChange={(e) => setForm((f) => ({ ...f, sortOrder: Number(e.target.value) }))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                        </FormField>
                        <FormField name="minPermissionLevel" label="Min Permission Level">
                            <select value={form.minPermissionLevel} onChange={(e) => setForm((f) => ({ ...f, minPermissionLevel: Number(e.target.value) }))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                                <option value={25}>25 — Viewer</option>
                                <option value={50}>50 — Member</option>
                                <option value={75}>75 — DeptLead</option>
                                <option value={100}>100 — OrgAdmin</option>
                            </select>
                        </FormField>
                    </div>
                    {!editingId && (
                        <FormField name="parentId" label="Parent Item (optional)">
                            <select value={form.parentId} onChange={(e) => setForm((f) => ({ ...f, parentId: e.target.value }))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                                <option value="">None (top-level)</option>
                                {items.map((i) => <option key={i.navigationItemId} value={i.navigationItemId}>{i.label}</option>)}
                            </select>
                        </FormField>
                    )}
                    <label className="flex items-center gap-2 text-sm font-medium text-foreground">
                        <input type="checkbox" checked={form.isEnabled} onChange={(e) => setForm((f) => ({ ...f, isEnabled: e.target.checked }))} className="rounded border-input" />
                        Enabled
                    </label>
                    <div className="flex justify-end gap-2 pt-2">
                        <button onClick={() => setModalOpen(false)} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">Cancel</button>
                        <button onClick={handleSave} disabled={saving || !form.label || !form.path} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                            {saving ? 'Saving...' : editingId ? 'Update' : 'Create'}
                        </button>
                    </div>
                </div>
            </Modal>

            {/* Delete Confirm */}
            <ConfirmDialog
                open={!!deleteTarget}
                title="Delete Navigation Item"
                message="This will remove the navigation item. Child items will also be deleted. Are you sure?"
                onConfirm={handleDelete}
                onCancel={() => setDeleteTarget(null)}
            />
        </div>
    );
}
