import { useState, useEffect, useCallback } from 'react';
import { useRiskRegisterStore } from '@/stores/riskRegisterStore';
import type { CreateRiskRequest, UpdateRiskRequest } from '@/types/analytics';

interface RiskRegisterTableProps {
    projectId: string;
}

const SEVERITIES = ['Low', 'Medium', 'High', 'Critical'] as const;
const LIKELIHOODS = ['Low', 'Medium', 'High'] as const;
const STATUSES = ['Open', 'Mitigating', 'Mitigated', 'Accepted'] as const;

function severityBadge(severity: string) {
    const colors: Record<string, string> = {
        Critical: 'bg-red-100 text-red-800',
        High: 'bg-orange-100 text-orange-800',
        Medium: 'bg-yellow-100 text-yellow-800',
        Low: 'bg-green-100 text-green-800',
    };
    return (
        <span className={`inline-block rounded px-1.5 py-0.5 text-xs font-medium ${colors[severity] ?? 'bg-muted text-muted-foreground'}`}>
            {severity}
        </span>
    );
}

const emptyForm: CreateRiskRequest = {
    projectId: '',
    title: '',
    severity: 'Medium',
    likelihood: 'Medium',
    mitigationStatus: 'Open',
};

export function RiskRegisterTable({ projectId }: RiskRegisterTableProps) {
    const [page, setPage] = useState(1);
    const [severityFilter, setSeverityFilter] = useState('');
    const [statusFilter, setStatusFilter] = useState('');
    const [showForm, setShowForm] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [form, setForm] = useState<CreateRiskRequest>({ ...emptyForm, projectId });
    const pageSize = 10;

    const { risks, totalCount, loading, error, fetchRisks, createRisk, updateRisk, deleteRisk } =
        useRiskRegisterStore();

    const reload = useCallback(() => {
        fetchRisks(
            projectId,
            undefined,
            severityFilter || undefined,
            statusFilter || undefined,
            page,
            pageSize,
        );
    }, [projectId, severityFilter, statusFilter, page, fetchRisks]);

    useEffect(() => {
        reload();
    }, [reload]);

    const handleSubmit = async () => {
        if (!form.title.trim()) return;
        if (editingId) {
            const update: UpdateRiskRequest = {
                title: form.title,
                description: form.description,
                severity: form.severity,
                likelihood: form.likelihood,
                mitigationStatus: form.mitigationStatus,
            };
            await updateRisk(editingId, update);
        } else {
            await createRisk({ ...form, projectId });
        }
        setShowForm(false);
        setEditingId(null);
        setForm({ ...emptyForm, projectId });
        reload();
    };

    const handleEdit = (riskId: string) => {
        const risk = risks.find((r) => r.riskRegisterId === riskId);
        if (!risk) return;
        setForm({
            projectId: risk.projectId,
            title: risk.title,
            description: risk.description,
            severity: risk.severity,
            likelihood: risk.likelihood,
            mitigationStatus: risk.mitigationStatus,
        });
        setEditingId(riskId);
        setShowForm(true);
    };

    const handleDelete = async (riskId: string) => {
        await deleteRisk(riskId);
        reload();
    };

    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-foreground">Risk Register</h3>
                <button
                    onClick={() => {
                        setShowForm(true);
                        setEditingId(null);
                        setForm({ ...emptyForm, projectId });
                    }}
                    className="rounded-md bg-primary px-3 py-1 text-xs font-medium text-primary-foreground hover:bg-primary/90"
                >
                    + Add Risk
                </button>
            </div>

            <div className="flex flex-wrap gap-3">
                <select
                    value={severityFilter}
                    onChange={(e) => { setSeverityFilter(e.target.value); setPage(1); }}
                    className="rounded-md border border-input bg-background px-2 py-1 text-sm"
                    aria-label="Filter by severity"
                >
                    <option value="">All Severities</option>
                    {SEVERITIES.map((s) => (
                        <option key={s} value={s}>{s}</option>
                    ))}
                </select>
                <select
                    value={statusFilter}
                    onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
                    className="rounded-md border border-input bg-background px-2 py-1 text-sm"
                    aria-label="Filter by status"
                >
                    <option value="">All Statuses</option>
                    {STATUSES.map((s) => (
                        <option key={s} value={s}>{s}</option>
                    ))}
                </select>
            </div>

            {showForm && (
                <div className="rounded-lg border border-border bg-card p-4 space-y-3">
                    <h4 className="text-sm font-medium text-foreground">
                        {editingId ? 'Edit Risk' : 'New Risk'}
                    </h4>
                    <input
                        type="text"
                        placeholder="Title"
                        value={form.title}
                        onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
                        className="w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm"
                        maxLength={200}
                    />
                    <textarea
                        placeholder="Description (optional)"
                        value={form.description ?? ''}
                        onChange={(e) => setForm((f) => ({ ...f, description: e.target.value || null }))}
                        className="w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm"
                        rows={2}
                    />
                    <div className="flex flex-wrap gap-3">
                        <select
                            value={form.severity}
                            onChange={(e) => setForm((f) => ({ ...f, severity: e.target.value }))}
                            className="rounded-md border border-input bg-background px-2 py-1 text-sm"
                            aria-label="Severity"
                        >
                            {SEVERITIES.map((s) => (
                                <option key={s} value={s}>{s}</option>
                            ))}
                        </select>
                        <select
                            value={form.likelihood}
                            onChange={(e) => setForm((f) => ({ ...f, likelihood: e.target.value }))}
                            className="rounded-md border border-input bg-background px-2 py-1 text-sm"
                            aria-label="Likelihood"
                        >
                            {LIKELIHOODS.map((l) => (
                                <option key={l} value={l}>{l}</option>
                            ))}
                        </select>
                        <select
                            value={form.mitigationStatus}
                            onChange={(e) => setForm((f) => ({ ...f, mitigationStatus: e.target.value }))}
                            className="rounded-md border border-input bg-background px-2 py-1 text-sm"
                            aria-label="Mitigation status"
                        >
                            {STATUSES.map((s) => (
                                <option key={s} value={s}>{s}</option>
                            ))}
                        </select>
                    </div>
                    <div className="flex gap-2">
                        <button
                            onClick={handleSubmit}
                            className="rounded-md bg-primary px-3 py-1 text-xs font-medium text-primary-foreground hover:bg-primary/90"
                        >
                            {editingId ? 'Update' : 'Create'}
                        </button>
                        <button
                            onClick={() => { setShowForm(false); setEditingId(null); }}
                            className="rounded-md border border-input px-3 py-1 text-xs text-muted-foreground hover:bg-accent"
                        >
                            Cancel
                        </button>
                    </div>
                </div>
            )}

            {loading && <p className="text-sm text-muted-foreground">Loading…</p>}
            {error && <p className="text-sm text-destructive">{error}</p>}

            {!loading && !error && risks.length === 0 && (
                <p className="text-sm text-muted-foreground">No risks found.</p>
            )}

            {!loading && !error && risks.length > 0 && (
                <>
                    <div className="overflow-x-auto">
                        <table className="w-full text-sm">
                            <thead>
                                <tr className="border-b border-border text-left text-xs text-muted-foreground">
                                    <th className="pb-2 pr-4">Title</th>
                                    <th className="pb-2 pr-4">Severity</th>
                                    <th className="pb-2 pr-4">Likelihood</th>
                                    <th className="pb-2 pr-4">Status</th>
                                    <th className="pb-2 text-right">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {risks.map((r) => (
                                    <tr key={r.riskRegisterId} className="border-b border-border/50">
                                        <td className="py-2 pr-4 font-medium text-foreground">{r.title}</td>
                                        <td className="py-2 pr-4">{severityBadge(r.severity)}</td>
                                        <td className="py-2 pr-4 text-muted-foreground">{r.likelihood}</td>
                                        <td className="py-2 pr-4 text-muted-foreground">{r.mitigationStatus}</td>
                                        <td className="py-2 text-right">
                                            <button
                                                onClick={() => handleEdit(r.riskRegisterId)}
                                                className="mr-2 text-xs text-primary hover:underline"
                                            >
                                                Edit
                                            </button>
                                            <button
                                                onClick={() => handleDelete(r.riskRegisterId)}
                                                className="text-xs text-destructive hover:underline"
                                            >
                                                Delete
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    <div className="flex items-center justify-between text-xs text-muted-foreground">
                        <span>
                            Page {page} of {totalPages} ({totalCount} total)
                        </span>
                        <div className="flex gap-2">
                            <button
                                disabled={page <= 1}
                                onClick={() => setPage((p) => p - 1)}
                                className="rounded border border-input px-2 py-1 hover:bg-accent disabled:opacity-50"
                            >
                                Prev
                            </button>
                            <button
                                disabled={page >= totalPages}
                                onClick={() => setPage((p) => p + 1)}
                                className="rounded border border-input px-2 py-1 hover:bg-accent disabled:opacity-50"
                            >
                                Next
                            </button>
                        </div>
                    </div>
                </>
            )}
        </div>
    );
}
