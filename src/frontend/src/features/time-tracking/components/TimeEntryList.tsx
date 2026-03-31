import { useState, useEffect, useCallback } from 'react';
import { Plus, Pencil, Trash2, Check, X } from 'lucide-react';
import { useTimeTrackingStore } from '@/stores/timeTrackingStore';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Pagination } from '@/components/common/Pagination';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import type { TimeEntryResponse, CreateTimeEntryRequest, UpdateTimeEntryRequest } from '@/types/timeTracking';

const STATUSES = ['Pending', 'Approved', 'Rejected'] as const;

export function TimeEntryList() {
    const { addToast } = useToast();
    const {
        timeEntries, totalCount, loading,
        fetchTimeEntries, createTimeEntry, updateTimeEntry, deleteTimeEntry,
        approveTimeEntry, rejectTimeEntry,
    } = useTimeTrackingStore();

    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);

    // Filters
    const [storyId, setStoryId] = useState('');
    const [projectId, setProjectId] = useState('');
    const [sprintId, setSprintId] = useState('');
    const [memberId, setMemberId] = useState('');
    const [dateFrom, setDateFrom] = useState('');
    const [dateTo, setDateTo] = useState('');
    const [billableFilter, setBillableFilter] = useState('');
    const [statusFilter, setStatusFilter] = useState('');

    // Modal
    const [modalOpen, setModalOpen] = useState(false);
    const [editingEntry, setEditingEntry] = useState<TimeEntryResponse | null>(null);
    const [formStoryId, setFormStoryId] = useState('');
    const [formDate, setFormDate] = useState('');
    const [formHours, setFormHours] = useState('');
    const [formDescription, setFormDescription] = useState('');
    const [formBillable, setFormBillable] = useState(true);

    // Reject modal
    const [rejectTarget, setRejectTarget] = useState<string | null>(null);
    const [rejectReason, setRejectReason] = useState('');

    const load = useCallback(() => {
        fetchTimeEntries({
            storyId: storyId || undefined,
            projectId: projectId || undefined,
            sprintId: sprintId || undefined,
            memberId: memberId || undefined,
            dateFrom: dateFrom || undefined,
            dateTo: dateTo || undefined,
            billable: billableFilter === '' ? undefined : billableFilter === 'true',
            status: statusFilter || undefined,
            page,
            pageSize,
        });
    }, [storyId, projectId, sprintId, memberId, dateFrom, dateTo, billableFilter, statusFilter, page, pageSize, fetchTimeEntries]);

    useEffect(() => { load(); }, [load]);

    const openCreate = () => {
        setEditingEntry(null);
        setFormStoryId('');
        setFormDate(new Date().toISOString().slice(0, 10));
        setFormHours('');
        setFormDescription('');
        setFormBillable(true);
        setModalOpen(true);
    };

    const openEdit = (entry: TimeEntryResponse) => {
        setEditingEntry(entry);
        setFormStoryId(entry.storyId);
        setFormDate(entry.date.slice(0, 10));
        setFormHours(String(entry.hours));
        setFormDescription(entry.description ?? '');
        setFormBillable(entry.billable);
        setModalOpen(true);
    };

    const handleSubmit = async () => {
        try {
            if (editingEntry) {
                const data: UpdateTimeEntryRequest = {
                    date: formDate,
                    hours: parseFloat(formHours),
                    description: formDescription || null,
                    billable: formBillable,
                };
                await updateTimeEntry(editingEntry.timeEntryId, data);
                addToast('success', 'Time entry updated');
            } else {
                const data: CreateTimeEntryRequest = {
                    storyId: formStoryId,
                    date: formDate,
                    hours: parseFloat(formHours),
                    description: formDescription || null,
                    billable: formBillable,
                };
                await createTimeEntry(data);
                addToast('success', 'Time entry created');
            }
            setModalOpen(false);
            load();
        } catch {
            addToast('error', 'Failed to save time entry');
        }
    };

    const handleDelete = async (id: string) => {
        try {
            await deleteTimeEntry(id);
            addToast('success', 'Time entry deleted');
            load();
        } catch {
            addToast('error', 'Failed to delete time entry');
        }
    };

    const handleApprove = async (id: string) => {
        try {
            await approveTimeEntry(id);
            addToast('success', 'Time entry approved');
            load();
        } catch {
            addToast('error', 'Failed to approve time entry');
        }
    };

    const handleReject = async () => {
        if (!rejectTarget) return;
        try {
            await rejectTimeEntry(rejectTarget, { reason: rejectReason });
            addToast('success', 'Time entry rejected');
            setRejectTarget(null);
            setRejectReason('');
            load();
        } catch {
            addToast('error', 'Failed to reject time entry');
        }
    };

    const columns: Column<TimeEntryResponse>[] = [
        { key: 'date', header: 'Date', render: (r) => r.date.slice(0, 10) },
        { key: 'storyKey', header: 'Story' },
        { key: 'projectName', header: 'Project' },
        { key: 'memberName', header: 'Member' },
        { key: 'hours', header: 'Hours' },
        { key: 'billable', header: 'Billable', render: (r) => r.billable ? 'Yes' : 'No' },
        { key: 'status', header: 'Status', render: (r) => <Badge variant="status" value={r.status} /> },
        {
            key: 'actions', header: 'Actions', render: (r) => (
                <div className="flex items-center gap-1">
                    <button onClick={() => openEdit(r)} className="rounded p-1 text-muted-foreground hover:bg-accent" title="Edit"><Pencil size={14} /></button>
                    <button onClick={() => handleDelete(r.timeEntryId)} className="rounded p-1 text-destructive hover:bg-destructive/10" title="Delete"><Trash2 size={14} /></button>
                    {r.status === 'Pending' && (
                        <>
                            <button onClick={() => handleApprove(r.timeEntryId)} className="rounded p-1 text-green-600 hover:bg-green-600/10" title="Approve"><Check size={14} /></button>
                            <button onClick={() => { setRejectTarget(r.timeEntryId); setRejectReason(''); }} className="rounded p-1 text-destructive hover:bg-destructive/10" title="Reject"><X size={14} /></button>
                        </>
                    )}
                </div>
            ),
        },
    ];

    const inputCls = 'rounded-md border border-input bg-background px-2 py-1.5 text-sm';

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h2 className="text-lg font-semibold text-foreground">Time Entries</h2>
                <button onClick={openCreate} className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90">
                    <Plus size={14} /> New Entry
                </button>
            </div>

            {/* Filters */}
            <div className="flex flex-wrap gap-2">
                <input className={inputCls} placeholder="Story ID" value={storyId} onChange={(e) => { setStoryId(e.target.value); setPage(1); }} />
                <input className={inputCls} placeholder="Project ID" value={projectId} onChange={(e) => { setProjectId(e.target.value); setPage(1); }} />
                <input className={inputCls} placeholder="Sprint ID" value={sprintId} onChange={(e) => { setSprintId(e.target.value); setPage(1); }} />
                <input className={inputCls} placeholder="Member ID" value={memberId} onChange={(e) => { setMemberId(e.target.value); setPage(1); }} />
                <input className={inputCls} type="date" value={dateFrom} onChange={(e) => { setDateFrom(e.target.value); setPage(1); }} aria-label="Date from" />
                <input className={inputCls} type="date" value={dateTo} onChange={(e) => { setDateTo(e.target.value); setPage(1); }} aria-label="Date to" />
                <select className={inputCls} value={billableFilter} onChange={(e) => { setBillableFilter(e.target.value); setPage(1); }} aria-label="Billable">
                    <option value="">All</option>
                    <option value="true">Billable</option>
                    <option value="false">Non-billable</option>
                </select>
                <select className={inputCls} value={statusFilter} onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }} aria-label="Status">
                    <option value="">All Statuses</option>
                    {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
                </select>
            </div>

            <DataTable columns={columns} data={timeEntries} loading={loading} keyExtractor={(r) => r.timeEntryId} />
            <Pagination page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={setPage} onPageSizeChange={(s) => { setPageSize(s); setPage(1); }} />

            {/* Create / Edit Modal */}
            <Modal open={modalOpen} onClose={() => setModalOpen(false)} title={editingEntry ? 'Edit Time Entry' : 'New Time Entry'}>
                <div className="space-y-3">
                    {!editingEntry && (
                        <input className={`${inputCls} w-full`} placeholder="Story ID" value={formStoryId} onChange={(e) => setFormStoryId(e.target.value)} />
                    )}
                    <input className={`${inputCls} w-full`} type="date" value={formDate} onChange={(e) => setFormDate(e.target.value)} aria-label="Date" />
                    <input className={`${inputCls} w-full`} type="number" step="0.25" min="0" placeholder="Hours" value={formHours} onChange={(e) => setFormHours(e.target.value)} />
                    <input className={`${inputCls} w-full`} placeholder="Description" value={formDescription} onChange={(e) => setFormDescription(e.target.value)} />
                    <label className="flex items-center gap-2 text-sm text-foreground">
                        <input type="checkbox" checked={formBillable} onChange={(e) => setFormBillable(e.target.checked)} /> Billable
                    </label>
                    <div className="flex justify-end gap-2 pt-2">
                        <button onClick={() => setModalOpen(false)} className="rounded-md border border-input px-3 py-1.5 text-sm text-muted-foreground hover:bg-accent">Cancel</button>
                        <button onClick={handleSubmit} className="rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90">Save</button>
                    </div>
                </div>
            </Modal>

            {/* Reject Modal */}
            <Modal open={!!rejectTarget} onClose={() => setRejectTarget(null)} title="Reject Time Entry">
                <div className="space-y-3">
                    <textarea className={`${inputCls} w-full`} rows={3} placeholder="Rejection reason" value={rejectReason} onChange={(e) => setRejectReason(e.target.value)} />
                    <div className="flex justify-end gap-2 pt-2">
                        <button onClick={() => setRejectTarget(null)} className="rounded-md border border-input px-3 py-1.5 text-sm text-muted-foreground hover:bg-accent">Cancel</button>
                        <button onClick={handleReject} className="rounded-md bg-destructive px-3 py-1.5 text-sm font-medium text-destructive-foreground hover:bg-destructive/90">Reject</button>
                    </div>
                </div>
            </Modal>
        </div>
    );
}
