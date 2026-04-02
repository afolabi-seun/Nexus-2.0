import { useState, useEffect, useCallback } from 'react';
import { utilityApi } from '@/api/utilityApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Modal } from '@/components/common/Modal';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { useToast } from '@/components/common/Toast';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { DepartmentType, PriorityLevel, TaskTypeRef, WorkflowState } from '@/types/utility';
import { Database, Plus } from 'lucide-react';

type TabKey = 'departmentTypes' | 'priorityLevels' | 'taskTypes' | 'workflowStates';

const tabs: { key: TabKey; label: string }[] = [
    { key: 'departmentTypes', label: 'Department Types' },
    { key: 'priorityLevels', label: 'Priority Levels' },
    { key: 'taskTypes', label: 'Task Types' },
    { key: 'workflowStates', label: 'Workflow States' },
];

export function ReferenceDataPage() {
    const { addToast } = useToast();
    const [activeTab, setActiveTab] = useState<TabKey>('departmentTypes');
    const [loading, setLoading] = useState(true);
    const [departmentTypes, setDepartmentTypes] = useState<DepartmentType[]>([]);
    const [priorityLevels, setPriorityLevels] = useState<PriorityLevel[]>([]);
    const [taskTypes, setTaskTypes] = useState<TaskTypeRef[]>([]);
    const [workflowStates, setWorkflowStates] = useState<WorkflowState[]>([]);
    const [createModalOpen, setCreateModalOpen] = useState(false);
    const [saving, setSaving] = useState(false);
    const [formCode, setFormCode] = useState('');
    const [formName, setFormName] = useState('');
    const [formLevel, setFormLevel] = useState('');
    const [errors, setErrors] = useState<Record<string, string>>({});

    const fetchAll = useCallback(async () => {
        setLoading(true);
        try {
            const [dt, pl, tt, ws] = await Promise.all([
                utilityApi.getDepartmentTypes(),
                utilityApi.getPriorityLevels(),
                utilityApi.getTaskTypes(),
                utilityApi.getWorkflowStates(),
            ]);
            setDepartmentTypes(dt);
            setPriorityLevels(pl);
            setTaskTypes(tt);
            setWorkflowStates(ws);
        } catch {
            addToast('error', 'Failed to load reference data');
        } finally {
            setLoading(false);
        }
    }, [addToast]);

    useEffect(() => { fetchAll(); }, [fetchAll]);

    const openCreateModal = () => {
        setFormCode('');
        setFormName('');
        setFormLevel('');
        setErrors({});
        setCreateModalOpen(true);
    };

    const handleCreate = async () => {
        const newErrors: Record<string, string> = {};
        if (!formCode.trim()) newErrors.code = 'Code is required';
        if (!formName.trim()) newErrors.name = 'Name is required';
        if (activeTab === 'priorityLevels') {
            const lvl = Number(formLevel);
            if (!formLevel.trim() || isNaN(lvl)) newErrors.level = 'Level must be a number';
        }
        if (Object.keys(newErrors).length > 0) {
            setErrors(newErrors);
            return;
        }

        setSaving(true);
        try {
            if (activeTab === 'departmentTypes') {
                await utilityApi.createDepartmentType({ code: formCode.trim(), name: formName.trim() });
                addToast('success', 'Department type created');
                const updated = await utilityApi.getDepartmentTypes();
                setDepartmentTypes(updated);
            } else {
                await utilityApi.createPriorityLevel({ code: formCode.trim(), name: formName.trim(), level: Number(formLevel) });
                addToast('success', 'Priority level created');
                const updated = await utilityApi.getPriorityLevels();
                setPriorityLevels(updated);
            }
            setCreateModalOpen(false);
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', 'Failed to create entry');
            }
        } finally {
            setSaving(false);
        }
    };

    const deptColumns: Column<DepartmentType>[] = [
        { key: 'code', header: 'Code' },
        { key: 'name', header: 'Name' },
    ];

    const priorityColumns: Column<PriorityLevel>[] = [
        { key: 'code', header: 'Code' },
        { key: 'name', header: 'Name' },
        { key: 'level', header: 'Level', render: (row) => String(row.level) },
    ];

    const taskTypeColumns: Column<TaskTypeRef>[] = [
        { key: 'code', header: 'Code' },
        { key: 'name', header: 'Name' },
        { key: 'defaultDepartment', header: 'Default Department' },
    ];

    const workflowColumns: Column<WorkflowState>[] = [
        { key: 'entityType', header: 'Entity Type' },
        { key: 'status', header: 'Status' },
        {
            key: 'validTransitions',
            header: 'Valid Transitions',
            render: (row) => row.validTransitions.join(', '),
        },
    ];

    const canCreate = activeTab === 'departmentTypes' || activeTab === 'priorityLevels';
    const modalTitle = activeTab === 'departmentTypes' ? 'Add Department Type' : 'Add Priority Level';

    return (
        <div className="space-y-6">
            <h1 className="flex items-center gap-2 text-2xl font-semibold text-foreground">
                <Database size={24} /> Reference Data
            </h1>

            {/* Tab bar */}
            <div className="flex gap-1 border-b border-border">
                {tabs.map((tab) => (
                    <button
                        key={tab.key}
                        onClick={() => setActiveTab(tab.key)}
                        className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${activeTab === tab.key
                                ? 'border-primary text-foreground'
                                : 'border-transparent text-muted-foreground hover:text-foreground'
                            }`}
                    >
                        {tab.label}
                    </button>
                ))}
            </div>

            {/* Create button */}
            {canCreate && (
                <div className="flex justify-end">
                    <button
                        onClick={openCreateModal}
                        className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        <Plus size={14} /> {modalTitle}
                    </button>
                </div>
            )}

            {loading ? (
                <SkeletonLoader variant="table" rows={5} columns={3} />
            ) : (
                <>
                    {activeTab === 'departmentTypes' && (
                        <DataTable columns={deptColumns} data={departmentTypes} keyExtractor={(r) => r.code} />
                    )}
                    {activeTab === 'priorityLevels' && (
                        <DataTable columns={priorityColumns} data={priorityLevels} keyExtractor={(r) => r.code} />
                    )}
                    {activeTab === 'taskTypes' && (
                        <DataTable columns={taskTypeColumns} data={taskTypes} keyExtractor={(r) => r.code} />
                    )}
                    {activeTab === 'workflowStates' && (
                        <DataTable columns={workflowColumns} data={workflowStates} keyExtractor={(r) => `${r.entityType}-${r.status}`} />
                    )}
                </>
            )}

            {/* Create Modal */}
            <Modal open={createModalOpen} onClose={() => setCreateModalOpen(false)} title={modalTitle}>
                <div className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-foreground mb-1">Code</label>
                        <input
                            type="text"
                            value={formCode}
                            onChange={(e) => { setFormCode(e.target.value); setErrors((prev) => ({ ...prev, code: '' })); }}
                            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground"
                        />
                        {errors.code && <p className="mt-1 text-xs text-red-500">{errors.code}</p>}
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-foreground mb-1">Name</label>
                        <input
                            type="text"
                            value={formName}
                            onChange={(e) => { setFormName(e.target.value); setErrors((prev) => ({ ...prev, name: '' })); }}
                            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground"
                        />
                        {errors.name && <p className="mt-1 text-xs text-red-500">{errors.name}</p>}
                    </div>
                    {activeTab === 'priorityLevels' && (
                        <div>
                            <label className="block text-sm font-medium text-foreground mb-1">Level</label>
                            <input
                                type="number"
                                value={formLevel}
                                onChange={(e) => { setFormLevel(e.target.value); setErrors((prev) => ({ ...prev, level: '' })); }}
                                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground"
                            />
                            {errors.level && <p className="mt-1 text-xs text-red-500">{errors.level}</p>}
                        </div>
                    )}
                    <div className="flex justify-end gap-2 pt-2">
                        <button
                            onClick={() => setCreateModalOpen(false)}
                            className="rounded-md border border-input px-3 py-2 text-sm font-medium text-foreground hover:bg-accent"
                        >
                            Cancel
                        </button>
                        <button
                            onClick={handleCreate}
                            disabled={saving}
                            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                        >
                            {saving ? 'Saving...' : 'Create'}
                        </button>
                    </div>
                </div>
            </Modal>
        </div>
    );
}
