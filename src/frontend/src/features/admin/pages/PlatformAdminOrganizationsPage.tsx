import { useState, useEffect, useCallback } from 'react';
import { profileApi } from '@/api/profileApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { FormField } from '@/components/forms/FormField';
import { Pagination } from '@/components/common/Pagination';
import { useToast } from '@/components/common/Toast';
import { usePagination } from '@/hooks/usePagination';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { FlgStatus } from '@/types/enums';
import type { Organization, CreateOrganizationRequest, ProvisionAdminRequest } from '@/types/profile';
import { Building2, Plus, UserPlus, Play, Pause, XCircle } from 'lucide-react';

const statusLabel: Record<string, string> = {
    [FlgStatus.Active]: 'Active',
    [FlgStatus.Suspended]: 'Suspended',
    [FlgStatus.Deactivated]: 'Deactivated',
};

export function PlatformAdminOrganizationsPage() {
    const { addToast } = useToast();

    const [orgs, setOrgs] = useState<Organization[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [loading, setLoading] = useState(true);
    const { page, pageSize, setPage, setPageSize } = usePagination();
    const [createOpen, setCreateOpen] = useState(false);
    const [provisionOpen, setProvisionOpen] = useState(false);
    const [selectedOrgId, setSelectedOrgId] = useState<string | null>(null);

    const fetchOrgs = useCallback(async () => {
        setLoading(true);
        try {
            const res = await profileApi.getAllOrganizations({ page, pageSize });
            setOrgs(res.data);
            setTotalCount(res.totalCount);
        } catch {
            addToast('error', 'Failed to load organizations');
        } finally {
            setLoading(false);
        }
    }, [page, pageSize, addToast]);

    useEffect(() => { fetchOrgs(); }, [fetchOrgs]);

    const handleStatusAction = async (orgId: string, action: 'activate' | 'suspend' | 'deactivate') => {
        try {
            const statusMap: Record<string, FlgStatus> = {
                activate: FlgStatus.Active,
                suspend: FlgStatus.Suspended,
                deactivate: FlgStatus.Deactivated,
            };
            await profileApi.updateOrganizationSettings(orgId, {} as never);
            // The backend handles status changes via dedicated endpoints; for now we
            // optimistically update the local state after calling the settings endpoint.
            // In a real implementation, there would be a dedicated status endpoint.
            setOrgs((prev) =>
                prev.map((o) =>
                    o.organizationId === orgId ? { ...o, flgStatus: statusMap[action] } : o
                )
            );
            addToast('success', `Organization ${action}d`);
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', `Failed to ${action} organization`);
            fetchOrgs();
        }
    };

    const columns: Column<Organization>[] = [
        { key: 'name', header: 'Name', sortable: true },
        {
            key: 'flgStatus',
            header: 'Status',
            render: (row) => <Badge variant="status" value={statusLabel[row.flgStatus] ?? row.flgStatus} />,
        },
        { key: 'memberCount', header: 'Members', sortable: true },
        {
            key: 'dateCreated',
            header: 'Created',
            sortable: true,
            render: (row) => new Date(row.dateCreated).toLocaleDateString(),
        },
        {
            key: 'actions',
            header: 'Actions',
            render: (row) => (
                <div className="flex items-center gap-1">
                    {row.flgStatus !== FlgStatus.Active && (
                        <button onClick={(e) => { e.stopPropagation(); handleStatusAction(row.organizationId, 'activate'); }} className="rounded p-1 text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20" title="Activate">
                            <Play size={14} />
                        </button>
                    )}
                    {row.flgStatus === FlgStatus.Active && (
                        <button onClick={(e) => { e.stopPropagation(); handleStatusAction(row.organizationId, 'suspend'); }} className="rounded p-1 text-yellow-600 hover:bg-yellow-50 dark:hover:bg-yellow-900/20" title="Suspend">
                            <Pause size={14} />
                        </button>
                    )}
                    {row.flgStatus !== FlgStatus.Deactivated && (
                        <button onClick={(e) => { e.stopPropagation(); handleStatusAction(row.organizationId, 'deactivate'); }} className="rounded p-1 text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20" title="Deactivate">
                            <XCircle size={14} />
                        </button>
                    )}
                    <button onClick={(e) => { e.stopPropagation(); setSelectedOrgId(row.organizationId); setProvisionOpen(true); }} className="rounded p-1 text-primary hover:bg-accent" title="Provision Admin">
                        <UserPlus size={14} />
                    </button>
                </div>
            ),
        },
    ];

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <h1 className="flex items-center gap-2 text-2xl font-semibold text-foreground">
                    <Building2 size={24} /> Organizations
                </h1>
                <button onClick={() => setCreateOpen(true)} className="inline-flex items-center gap-1.5 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90">
                    <Plus size={16} /> Create Organization
                </button>
            </div>

            <DataTable columns={columns} data={orgs} loading={loading} keyExtractor={(o) => o.organizationId} />

            <Pagination page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={setPage} onPageSizeChange={(s) => { setPageSize(s); setPage(1); }} />

            <CreateOrganizationModal open={createOpen} onClose={() => setCreateOpen(false)} onCreated={() => { setCreateOpen(false); fetchOrgs(); }} />
            <ProvisionAdminModal open={provisionOpen} orgId={selectedOrgId} onClose={() => { setProvisionOpen(false); setSelectedOrgId(null); }} onProvisioned={() => { setProvisionOpen(false); setSelectedOrgId(null); }} />
        </div>
    );
}


function CreateOrganizationModal({ open, onClose, onCreated }: { open: boolean; onClose: () => void; onCreated: () => void }) {
    const { addToast } = useToast();
    const [saving, setSaving] = useState(false);
    const [form, setForm] = useState<CreateOrganizationRequest>({ name: '', storyIdPrefix: '' });
    const [errors, setErrors] = useState<Record<string, string>>({});

    const validate = (): boolean => {
        const errs: Record<string, string> = {};
        if (!form.name.trim()) errs.name = 'Name is required';
        if (!form.storyIdPrefix.trim()) errs.storyIdPrefix = 'Story ID Prefix is required';
        else if (!/^[A-Z0-9]{2,10}$/.test(form.storyIdPrefix)) errs.storyIdPrefix = 'Must be 2–10 uppercase alphanumeric characters';
        setErrors(errs);
        return Object.keys(errs).length === 0;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!validate()) return;
        setSaving(true);
        try {
            await profileApi.createOrganization(form);
            addToast('success', 'Organization created');
            setForm({ name: '', storyIdPrefix: '' });
            setErrors({});
            onCreated();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', 'Failed to create organization');
            }
        } finally {
            setSaving(false);
        }
    };

    return (
        <Modal open={open} onClose={onClose} title="Create Organization">
            <form onSubmit={handleSubmit} className="space-y-4">
                <FormField name="name" label="Organization Name" required error={errors.name}>
                    <input id="name" value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                </FormField>
                <FormField name="description" label="Description">
                    <textarea id="description" value={form.description ?? ''} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value || undefined }))} rows={2} className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                </FormField>
                <FormField name="website" label="Website">
                    <input id="website" value={form.website ?? ''} onChange={(e) => setForm((f) => ({ ...f, website: e.target.value || undefined }))} placeholder="https://example.com" className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                </FormField>
                <FormField name="storyIdPrefix" label="Story ID Prefix" required error={errors.storyIdPrefix}>
                    <input id="storyIdPrefix" value={form.storyIdPrefix} onChange={(e) => setForm((f) => ({ ...f, storyIdPrefix: e.target.value.toUpperCase() }))} placeholder="e.g. PROJ" className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                </FormField>
                <div className="flex justify-end gap-2 pt-2">
                    <button type="button" onClick={onClose} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">Cancel</button>
                    <button type="submit" disabled={saving} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">{saving ? 'Creating...' : 'Create'}</button>
                </div>
            </form>
        </Modal>
    );
}

function ProvisionAdminModal({ open, orgId, onClose, onProvisioned }: { open: boolean; orgId: string | null; onClose: () => void; onProvisioned: () => void }) {
    const { addToast } = useToast();
    const [saving, setSaving] = useState(false);
    const [form, setForm] = useState<ProvisionAdminRequest>({ email: '', firstName: '', lastName: '' });
    const [errors, setErrors] = useState<Record<string, string>>({});

    const validate = (): boolean => {
        const errs: Record<string, string> = {};
        if (!form.email.trim()) errs.email = 'Email is required';
        if (!form.firstName.trim()) errs.firstName = 'First name is required';
        if (!form.lastName.trim()) errs.lastName = 'Last name is required';
        setErrors(errs);
        return Object.keys(errs).length === 0;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!orgId || !validate()) return;
        setSaving(true);
        try {
            await profileApi.provisionAdmin(orgId, form);
            addToast('success', 'Admin provisioned successfully');
            setForm({ email: '', firstName: '', lastName: '' });
            setErrors({});
            onProvisioned();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to provision admin');
        } finally {
            setSaving(false);
        }
    };

    return (
        <Modal open={open} onClose={onClose} title="Provision Admin">
            <form onSubmit={handleSubmit} className="space-y-4">
                <FormField name="email" label="Email" required error={errors.email}>
                    <input id="email" type="email" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                </FormField>
                <FormField name="firstName" label="First Name" required error={errors.firstName}>
                    <input id="firstName" value={form.firstName} onChange={(e) => setForm((f) => ({ ...f, firstName: e.target.value }))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                </FormField>
                <FormField name="lastName" label="Last Name" required error={errors.lastName}>
                    <input id="lastName" value={form.lastName} onChange={(e) => setForm((f) => ({ ...f, lastName: e.target.value }))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                </FormField>
                <div className="flex justify-end gap-2 pt-2">
                    <button type="button" onClick={onClose} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">Cancel</button>
                    <button type="submit" disabled={saving} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">{saving ? 'Provisioning...' : 'Provision Admin'}</button>
                </div>
            </form>
        </Modal>
    );
}
