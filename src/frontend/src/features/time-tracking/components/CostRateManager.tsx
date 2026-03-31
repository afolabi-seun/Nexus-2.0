import { useState, useEffect, useCallback } from 'react';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import { useTimeTrackingStore } from '@/stores/timeTrackingStore';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Pagination } from '@/components/common/Pagination';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import type { CostRateResponse, CreateCostRateRequest, UpdateCostRateRequest } from '@/types/timeTracking';

const RATE_TYPES = ['Member', 'Department', 'Organization'] as const;

export function CostRateManager() {
    const { addToast } = useToast();
    const { costRates, costRateTotalCount, loading, fetchCostRates, createCostRate, updateCostRate, deleteCostRate } = useTimeTrackingStore();

    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);
    const [rateTypeFilter, setRateTypeFilter] = useState('');
    const [memberIdFilter, setMemberIdFilter] = useState('');

    // Modal
    const [modalOpen, setModalOpen] = useState(false);
    const [editing, setEditing] = useState<CostRateResponse | null>(null);
    const [formRateType, setFormRateType] = useState('Member');
    const [formMemberId, setFormMemberId] = useState('');
    const [formDepartmentId, setFormDepartmentId] = useState('');
    const [formHourlyRate, setFormHourlyRate] = useState('');
    const [formCurrency, setFormCurrency] = useState('USD');
    const [formEffectiveFrom, setFormEffectiveFrom] = useState('');
    const [formEffectiveTo, setFormEffectiveTo] = useState('');

    const load = useCallback(() => {
        fetchCostRates({
            rateType: rateTypeFilter || undefined,
            memberId: memberIdFilter || undefined,
            page,
            pageSize,
        });
    }, [rateTypeFilter, memberIdFilter, page, pageSize, fetchCostRates]);

    useEffect(() => { load(); }, [load]);

    const openCreate = () => {
        setEditing(null);
        setFormRateType('Member');
        setFormMemberId('');
        setFormDepartmentId('');
        setFormHourlyRate('');
        setFormCurrency('USD');
        setFormEffectiveFrom(new Date().toISOString().slice(0, 10));
        setFormEffectiveTo('');
        setModalOpen(true);
    };

    const openEdit = (rate: CostRateResponse) => {
        setEditing(rate);
        setFormRateType(rate.rateType);
        setFormMemberId(rate.memberId ?? '');
        setFormDepartmentId(rate.departmentId ?? '');
        setFormHourlyRate(String(rate.hourlyRate));
        setFormCurrency(rate.currency);
        setFormEffectiveFrom(rate.effectiveFrom.slice(0, 10));
        setFormEffectiveTo(rate.effectiveTo?.slice(0, 10) ?? '');
        setModalOpen(true);
    };

    const handleSubmit = async () => {
        try {
            if (editing) {
                const data: UpdateCostRateRequest = {
                    hourlyRate: parseFloat(formHourlyRate),
                    currency: formCurrency,
                    effectiveFrom: formEffectiveFrom,
                    effectiveTo: formEffectiveTo || null,
                };
                await updateCostRate(editing.costRateId, data);
                addToast('success', 'Cost rate updated');
            } else {
                const data: CreateCostRateRequest = {
                    rateType: formRateType,
                    memberId: formMemberId || null,
                    departmentId: formDepartmentId || null,
                    hourlyRate: parseFloat(formHourlyRate),
                    currency: formCurrency,
                    effectiveFrom: formEffectiveFrom,
                    effectiveTo: formEffectiveTo || null,
                };
                await createCostRate(data);
                addToast('success', 'Cost rate created');
            }
            setModalOpen(false);
            load();
        } catch {
            addToast('error', 'Failed to save cost rate');
        }
    };

    const handleDelete = async (id: string) => {
        try {
            await deleteCostRate(id);
            addToast('success', 'Cost rate deleted');
            load();
        } catch {
            addToast('error', 'Failed to delete cost rate');
        }
    };

    const columns: Column<CostRateResponse>[] = [
        { key: 'rateType', header: 'Type' },
        { key: 'memberName', header: 'Member', render: (r) => r.memberName ?? '—' },
        { key: 'departmentName', header: 'Department', render: (r) => r.departmentName ?? '—' },
        { key: 'hourlyRate', header: 'Rate', render: (r) => `${r.currency} ${r.hourlyRate.toFixed(2)}` },
        { key: 'effectiveFrom', header: 'From', render: (r) => r.effectiveFrom.slice(0, 10) },
        { key: 'effectiveTo', header: 'To', render: (r) => r.effectiveTo?.slice(0, 10) ?? '—' },
        {
            key: 'actions', header: 'Actions', render: (r) => (
                <div className="flex items-center gap-1">
                    <button onClick={() => openEdit(r)} className="rounded p-1 text-muted-foreground hover:bg-accent" title="Edit"><Pencil size={14} /></button>
                    <button onClick={() => handleDelete(r.costRateId)} className="rounded p-1 text-destructive hover:bg-destructive/10" title="Delete"><Trash2 size={14} /></button>
                </div>
            ),
        },
    ];

    const inputCls = 'rounded-md border border-input bg-background px-2 py-1.5 text-sm';

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h2 className="text-lg font-semibold text-foreground">Cost Rates</h2>
                <button onClick={openCreate} className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90">
                    <Plus size={14} /> New Rate
                </button>
            </div>

            <div className="flex flex-wrap gap-2">
                <select className={inputCls} value={rateTypeFilter} onChange={(e) => { setRateTypeFilter(e.target.value); setPage(1); }} aria-label="Rate type">
                    <option value="">All Types</option>
                    {RATE_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
                </select>
                <input className={inputCls} placeholder="Member ID" value={memberIdFilter} onChange={(e) => { setMemberIdFilter(e.target.value); setPage(1); }} />
            </div>

            <DataTable columns={columns} data={costRates} loading={loading} keyExtractor={(r) => r.costRateId} />
            <Pagination page={page} pageSize={pageSize} totalCount={costRateTotalCount} onPageChange={setPage} onPageSizeChange={(s) => { setPageSize(s); setPage(1); }} />

            <Modal open={modalOpen} onClose={() => setModalOpen(false)} title={editing ? 'Edit Cost Rate' : 'New Cost Rate'}>
                <div className="space-y-3">
                    {!editing && (
                        <>
                            <select className={`${inputCls} w-full`} value={formRateType} onChange={(e) => setFormRateType(e.target.value)} aria-label="Rate type">
                                {RATE_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
                            </select>
                            {formRateType === 'Member' && <input className={`${inputCls} w-full`} placeholder="Member ID" value={formMemberId} onChange={(e) => setFormMemberId(e.target.value)} />}
                            {formRateType === 'Department' && <input className={`${inputCls} w-full`} placeholder="Department ID" value={formDepartmentId} onChange={(e) => setFormDepartmentId(e.target.value)} />}
                        </>
                    )}
                    <input className={`${inputCls} w-full`} type="number" step="0.01" min="0" placeholder="Hourly Rate" value={formHourlyRate} onChange={(e) => setFormHourlyRate(e.target.value)} />
                    <input className={`${inputCls} w-full`} placeholder="Currency" value={formCurrency} onChange={(e) => setFormCurrency(e.target.value)} />
                    <input className={`${inputCls} w-full`} type="date" value={formEffectiveFrom} onChange={(e) => setFormEffectiveFrom(e.target.value)} aria-label="Effective from" />
                    <input className={`${inputCls} w-full`} type="date" value={formEffectiveTo} onChange={(e) => setFormEffectiveTo(e.target.value)} aria-label="Effective to" />
                    <div className="flex justify-end gap-2 pt-2">
                        <button onClick={() => setModalOpen(false)} className="rounded-md border border-input px-3 py-1.5 text-sm text-muted-foreground hover:bg-accent">Cancel</button>
                        <button onClick={handleSubmit} className="rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90">Save</button>
                    </div>
                </div>
            </Modal>
        </div>
    );
}
