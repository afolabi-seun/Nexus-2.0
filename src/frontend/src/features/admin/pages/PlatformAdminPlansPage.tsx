import { useState, useEffect, useCallback } from 'react';
import { Package, Plus } from 'lucide-react';
import { adminBillingApi } from '@/api/adminBillingApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import { PlanFormModal } from '@/features/admin/components/PlanFormModal';
import type { AdminPlanResponse, AdminCreatePlanRequest, AdminUpdatePlanRequest } from '@/types/adminBilling';

export function PlatformAdminPlansPage() {
    const { addToast } = useToast();

    const [plans, setPlans] = useState<AdminPlanResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [formOpen, setFormOpen] = useState(false);
    const [editPlan, setEditPlan] = useState<AdminPlanResponse | null>(null);
    const [deactivateTarget, setDeactivateTarget] = useState<AdminPlanResponse | null>(null);
    const [deactivating, setDeactivating] = useState(false);

    const fetchPlans = useCallback(async () => {
        setLoading(true);
        try {
            const data = await adminBillingApi.getPlans();
            setPlans(data);
        } catch {
            addToast('error', 'Failed to load plans');
        } finally {
            setLoading(false);
        }
    }, [addToast]);

    useEffect(() => { fetchPlans(); }, [fetchPlans]);

    const handleCreate = async (data: AdminCreatePlanRequest) => {
        await adminBillingApi.createPlan(data);
        addToast('success', 'Plan created');
        fetchPlans();
    };

    const handleEdit = async (planId: string, data: AdminUpdatePlanRequest) => {
        await adminBillingApi.updatePlan(planId, data);
        addToast('success', 'Plan updated');
        fetchPlans();
    };

    const handleDeactivate = async () => {
        if (!deactivateTarget) return;
        setDeactivating(true);
        try {
            await adminBillingApi.deactivatePlan(deactivateTarget.planId);
            addToast('success', 'Plan deactivated');
            setDeactivateTarget(null);
            fetchPlans();
        } catch {
            addToast('error', 'Failed to deactivate plan');
        } finally {
            setDeactivating(false);
        }
    };

    const columns: Column<AdminPlanResponse>[] = [
        { key: 'planName', header: 'Name', sortable: true },
        { key: 'planCode', header: 'Code', sortable: true },
        { key: 'tierLevel', header: 'Tier', sortable: true },
        {
            key: 'priceMonthly',
            header: 'Monthly',
            render: (row) => `$${row.priceMonthly}`,
        },
        {
            key: 'priceYearly',
            header: 'Yearly',
            render: (row) => `$${row.priceYearly}`,
        },
        {
            key: 'limits',
            header: 'Limits',
            render: (row) => (
                <span className="text-xs text-muted-foreground">
                    {row.maxTeamMembers} members · {row.maxDepartments} depts · {row.maxStoriesPerMonth} stories
                </span>
            ),
        },
        {
            key: 'isActive',
            header: 'Status',
            render: (row) => (
                <Badge variant="status" value={row.isActive ? 'Active' : 'Deactivated'} />
            ),
        },
        {
            key: 'actions',
            header: 'Actions',
            render: (row) => (
                <div className="flex items-center gap-1">
                    <button
                        onClick={(e) => { e.stopPropagation(); setEditPlan(row); setFormOpen(true); }}
                        className="rounded px-2 py-1 text-xs font-medium text-primary hover:bg-primary/10"
                    >
                        Edit
                    </button>
                    {row.isActive && (
                        <button
                            onClick={(e) => { e.stopPropagation(); setDeactivateTarget(row); }}
                            className="rounded px-2 py-1 text-xs font-medium text-destructive hover:bg-destructive/10"
                        >
                            Deactivate
                        </button>
                    )}
                </div>
            ),
        },
    ];

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <h1 className="flex items-center gap-2 text-2xl font-semibold text-foreground">
                    <Package size={24} /> Plan Management
                </h1>
                <button
                    onClick={() => { setEditPlan(null); setFormOpen(true); }}
                    className="inline-flex items-center gap-1.5 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                >
                    <Plus size={16} /> Create Plan
                </button>
            </div>

            <DataTable
                columns={columns}
                data={plans}
                loading={loading}
                keyExtractor={(p) => p.planId}
            />

            <PlanFormModal
                open={formOpen}
                onClose={() => { setFormOpen(false); setEditPlan(null); }}
                onCreateSubmit={handleCreate}
                onEditSubmit={handleEdit}
                plan={editPlan}
            />

            {/* Deactivate confirmation */}
            <Modal
                open={!!deactivateTarget}
                onClose={() => setDeactivateTarget(null)}
                title="Deactivate Plan"
            >
                <div className="space-y-4">
                    <p className="text-sm text-muted-foreground">
                        Are you sure you want to deactivate <span className="font-medium text-foreground">{deactivateTarget?.planName}</span>?
                        Existing subscriptions will not be affected, but new organizations won't be able to select this plan.
                    </p>
                    <div className="flex justify-end gap-2">
                        <button onClick={() => setDeactivateTarget(null)} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">
                            Cancel
                        </button>
                        <button onClick={handleDeactivate} disabled={deactivating} className="rounded-md bg-destructive px-4 py-2 text-sm font-medium text-destructive-foreground hover:bg-destructive/90 disabled:opacity-50">
                            {deactivating ? 'Deactivating…' : 'Deactivate'}
                        </button>
                    </div>
                </div>
            </Modal>
        </div>
    );
}
