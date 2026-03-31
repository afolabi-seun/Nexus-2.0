import { useState, useEffect, useCallback } from 'react';
import { profileApi } from '@/api/profileApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { FormField } from '@/components/forms/FormField';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { useToast } from '@/components/common/Toast';
import { useAuth } from '@/hooks/useAuth';
import { useOrg } from '@/hooks/useOrg';
import { ListFilter } from '@/components/common/ListFilter';
import { useListFilters } from '@/hooks/useListFilters';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { FilterConfig } from '@/types/filters';
import type { Invite } from '@/types/profile';
import { Plus, X } from 'lucide-react';

const filterConfigs: FilterConfig[] = [
    {
        key: 'status',
        label: 'Status',
        type: 'select',
        options: [
            { value: 'Pending', label: 'Pending' },
            { value: 'Accepted', label: 'Accepted' },
            { value: 'Cancelled', label: 'Cancelled' },
            { value: 'Expired', label: 'Expired' },
        ],
    },
];

export function InviteManagementPage() {
    const { addToast } = useToast();
    const { user } = useAuth();
    const { departments } = useOrg();

    const [invites, setInvites] = useState<Invite[]>([]);
    const [loading, setLoading] = useState(true);
    const [createOpen, setCreateOpen] = useState(false);
    const [cancelTarget, setCancelTarget] = useState<string | null>(null);
    const [creating, setCreating] = useState(false);

    // Form state
    const [email, setEmail] = useState('');
    const [firstName, setFirstName] = useState('');
    const [lastName, setLastName] = useState('');
    const [deptId, setDeptId] = useState('');
    const [roleId, setRoleId] = useState('');
    const [errors, setErrors] = useState<Record<string, string>>({});

    const isDeptLead = user?.roleName === 'DeptLead';
    const filteredDepts = isDeptLead && user?.departmentId
        ? departments.filter((d) => d.departmentId === user.departmentId)
        : departments;

    const { filterValues, updateFilter, clearFilters, hasActiveFilters, activeFilterCount } =
        useListFilters(filterConfigs);

    const fetchInvites = useCallback(async () => {
        setLoading(true);
        try {
            const data = await profileApi.getInvites();
            const statusFilter = filterValues.status as string | undefined;
            setInvites(statusFilter ? data.filter((i) => i.status === statusFilter) : data);
        } catch {
            addToast('error', 'Failed to load invites');
        } finally {
            setLoading(false);
        }
    }, [filterValues, addToast]);

    useEffect(() => { fetchInvites(); }, [fetchInvites]);

    const handleCreate = async () => {
        const errs: Record<string, string> = {};
        if (!email.trim()) errs.email = 'Email is required';
        if (!firstName.trim()) errs.firstName = 'First name is required';
        if (!lastName.trim()) errs.lastName = 'Last name is required';
        if (!deptId) errs.deptId = 'Department is required';
        if (!roleId) errs.roleId = 'Role is required';
        if (Object.keys(errs).length > 0) { setErrors(errs); return; }

        setCreating(true);
        try {
            await profileApi.createInvite({ email: email.trim(), firstName: firstName.trim(), lastName: lastName.trim(), departmentId: deptId, roleId });
            addToast('success', 'Invite sent');
            setCreateOpen(false);
            resetForm();
            fetchInvites();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to create invite');
        } finally {
            setCreating(false);
        }
    };

    const handleCancel = async () => {
        if (!cancelTarget) return;
        try {
            await profileApi.cancelInvite(cancelTarget);
            addToast('success', 'Invite cancelled');
            setCancelTarget(null);
            fetchInvites();
        } catch {
            addToast('error', 'Failed to cancel invite');
        }
    };

    const resetForm = () => {
        setEmail(''); setFirstName(''); setLastName(''); setDeptId(''); setRoleId(''); setErrors({});
    };

    const columns: Column<Invite>[] = [
        { key: 'email', header: 'Email' },
        { key: 'name', header: 'Name', render: (i) => `${i.firstName} ${i.lastName}` },
        { key: 'departmentName', header: 'Department' },
        { key: 'roleName', header: 'Role', render: (i) => <Badge variant="role" value={i.roleName} /> },
        { key: 'expiryDate', header: 'Expires', render: (i) => new Date(i.expiryDate).toLocaleDateString() },
        { key: 'status', header: 'Status', render: (i) => <Badge variant="status" value={i.status} /> },
        {
            key: 'actions', header: '', render: (i) => i.status === 'Pending' ? (
                <button onClick={(e) => { e.stopPropagation(); setCancelTarget(i.inviteId); }} className="inline-flex items-center gap-1 text-xs text-destructive hover:underline">
                    <X size={12} /> Cancel
                </button>
            ) : null
        },
    ];

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-semibold text-foreground">Invitations</h1>
                <button onClick={() => setCreateOpen(true)} className="inline-flex items-center gap-1.5 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90">
                    <Plus size={16} /> Create Invite
                </button>
            </div>

            <ListFilter
                configs={filterConfigs}
                values={filterValues}
                onUpdateFilter={updateFilter}
                onClearFilters={clearFilters}
                hasActiveFilters={hasActiveFilters}
                activeFilterCount={activeFilterCount}
            />

            <DataTable columns={columns} data={invites} loading={loading} keyExtractor={(i) => i.inviteId} />

            <Modal open={createOpen} onClose={() => { setCreateOpen(false); resetForm(); }} title="Create Invite">
                <div className="space-y-4">
                    <FormField name="email" label="Email" error={errors.email} required>
                        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                    </FormField>
                    <div className="grid grid-cols-2 gap-4">
                        <FormField name="firstName" label="First Name" error={errors.firstName} required>
                            <input value={firstName} onChange={(e) => setFirstName(e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                        </FormField>
                        <FormField name="lastName" label="Last Name" error={errors.lastName} required>
                            <input value={lastName} onChange={(e) => setLastName(e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                        </FormField>
                    </div>
                    <FormField name="deptId" label="Department" error={errors.deptId} required>
                        <select value={deptId} onChange={(e) => setDeptId(e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                            <option value="">Select Department</option>
                            {filteredDepts.map((d) => <option key={d.departmentId} value={d.departmentId}>{d.name}</option>)}
                        </select>
                    </FormField>
                    <FormField name="roleId" label="Role" error={errors.roleId} required>
                        <select value={roleId} onChange={(e) => setRoleId(e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                            <option value="">Select Role</option>
                            <option value="OrgAdmin">OrgAdmin</option>
                            <option value="DeptLead">DeptLead</option>
                            <option value="Member">Member</option>
                            <option value="Viewer">Viewer</option>
                        </select>
                    </FormField>
                    <div className="flex justify-end gap-2">
                        <button onClick={() => { setCreateOpen(false); resetForm(); }} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">Cancel</button>
                        <button onClick={handleCreate} disabled={creating} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                            {creating ? 'Sending...' : 'Send Invite'}
                        </button>
                    </div>
                </div>
            </Modal>

            <ConfirmDialog
                open={cancelTarget !== null}
                onConfirm={handleCancel}
                onCancel={() => setCancelTarget(null)}
                title="Cancel Invite"
                message="Are you sure you want to cancel this invitation?"
                confirmLabel="Cancel Invite"
            />
        </div>
    );
}
