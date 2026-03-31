import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { profileApi } from '@/api/profileApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { FormField } from '@/components/forms/FormField';
import { useToast } from '@/components/common/Toast';
import { useAuth } from '@/hooks/useAuth';
import { ListFilter } from '@/components/common/ListFilter';
import { useListFilters } from '@/hooks/useListFilters';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { FilterConfig } from '@/types/filters';
import type { Department } from '@/types/profile';
import { Plus } from 'lucide-react';

const filterConfigs: FilterConfig[] = [
    {
        key: 'status',
        label: 'Status',
        type: 'select',
        options: [
            { value: 'A', label: 'Active' },
            { value: 'I', label: 'Inactive' },
        ],
    },
];

export function DepartmentListPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const { user } = useAuth();
    const isOrgAdmin = user?.roleName === 'OrgAdmin';

    const [departments, setDepartments] = useState<Department[]>([]);
    const [loading, setLoading] = useState(true);
    const [createOpen, setCreateOpen] = useState(false);
    const [formName, setFormName] = useState('');
    const [formCode, setFormCode] = useState('');
    const [formDesc, setFormDesc] = useState('');
    const [creating, setCreating] = useState(false);
    const [errors, setErrors] = useState<Record<string, string>>({});

    const { filterValues, updateFilter, clearFilters, hasActiveFilters, activeFilterCount } =
        useListFilters(filterConfigs);

    const fetchDepartments = useCallback(async () => {
        setLoading(true);
        try {
            const res = await profileApi.getDepartments({
                status: filterValues.status as string | undefined,
            } as Record<string, unknown>);
            setDepartments(res.data);
        } catch {
            addToast('error', 'Failed to load departments');
        } finally {
            setLoading(false);
        }
    }, [filterValues, addToast]);

    useEffect(() => { fetchDepartments(); }, [fetchDepartments]);

    const handleCreate = async () => {
        const errs: Record<string, string> = {};
        if (!formName.trim()) errs.name = 'Name is required';
        if (!formCode.trim()) errs.code = 'Code is required';
        if (Object.keys(errs).length > 0) { setErrors(errs); return; }

        setCreating(true);
        try {
            await profileApi.createDepartment({ name: formName.trim(), code: formCode.trim(), description: formDesc.trim() || undefined });
            addToast('success', 'Department created');
            setCreateOpen(false);
            setFormName(''); setFormCode(''); setFormDesc(''); setErrors({});
            fetchDepartments();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to create department');
        } finally {
            setCreating(false);
        }
    };

    const columns: Column<Department>[] = [
        { key: 'name', header: 'Name' },
        { key: 'code', header: 'Code' },
        { key: 'memberCount', header: 'Members', render: (d) => String(d.memberCount) },
        { key: 'isDefault', header: 'Default', render: (d) => d.isDefault ? <Badge value="Default" /> : <>—</> },
        { key: 'flgStatus', header: 'Status', render: (d) => <Badge variant="status" value={d.flgStatus === 'A' ? 'Active' : 'Inactive'} /> },
    ];

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-semibold text-foreground">Departments</h1>
                {isOrgAdmin && (
                    <button onClick={() => setCreateOpen(true)} className="inline-flex items-center gap-1.5 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90">
                        <Plus size={16} /> Create Department
                    </button>
                )}
            </div>

            <ListFilter
                configs={filterConfigs}
                values={filterValues}
                onUpdateFilter={updateFilter}
                onClearFilters={clearFilters}
                hasActiveFilters={hasActiveFilters}
                activeFilterCount={activeFilterCount}
            />

            <DataTable columns={columns} data={departments} loading={loading} keyExtractor={(d) => d.departmentId} onRowClick={(d) => navigate(`/departments/${d.departmentId}`)} />

            <Modal open={createOpen} onClose={() => setCreateOpen(false)} title="Create Department">
                <div className="space-y-4">
                    <FormField name="name" label="Name" error={errors.name} required>
                        <input value={formName} onChange={(e) => setFormName(e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                    </FormField>
                    <FormField name="code" label="Code" error={errors.code} required>
                        <input value={formCode} onChange={(e) => setFormCode(e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                    </FormField>
                    <FormField name="description" label="Description">
                        <textarea value={formDesc} onChange={(e) => setFormDesc(e.target.value)} rows={3} className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring resize-none" />
                    </FormField>
                    <div className="flex justify-end gap-2">
                        <button onClick={() => setCreateOpen(false)} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">Cancel</button>
                        <button onClick={handleCreate} disabled={creating} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                            {creating ? 'Creating...' : 'Create'}
                        </button>
                    </div>
                </div>
            </Modal>
        </div>
    );
}
