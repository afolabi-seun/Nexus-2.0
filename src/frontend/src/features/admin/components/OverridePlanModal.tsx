import { useState, useEffect } from 'react';
import { Modal } from '@/components/common/Modal';
import { FormField } from '@/components/forms/FormField';
import type { AdminPlanResponse } from '@/types/adminBilling';

interface OverridePlanModalProps {
    open: boolean;
    onClose: () => void;
    onSubmit: (planId: string, reason?: string) => Promise<void>;
    plans: AdminPlanResponse[];
    currentPlanId?: string;
}

export function OverridePlanModal({ open, onClose, onSubmit, plans, currentPlanId }: OverridePlanModalProps) {
    const [planId, setPlanId] = useState('');
    const [reason, setReason] = useState('');
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (open) {
            setPlanId('');
            setReason('');
            setError(null);
        }
    }, [open]);

    const activePlans = plans.filter((p) => p.isActive && p.planId !== currentPlanId);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!planId) {
            setError('Please select a plan');
            return;
        }
        setSaving(true);
        setError(null);
        try {
            await onSubmit(planId, reason || undefined);
            onClose();
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to override plan');
        } finally {
            setSaving(false);
        }
    };

    return (
        <Modal open={open} onClose={onClose} title="Override Subscription Plan">
            <form onSubmit={handleSubmit} className="space-y-4">
                {error && (
                    <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive" role="alert">
                        {error}
                    </p>
                )}
                <FormField name="planId" label="Target Plan" required>
                    <select
                        id="planId"
                        value={planId}
                        onChange={(e) => setPlanId(e.target.value)}
                        className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    >
                        <option value="">Select a plan…</option>
                        {activePlans.map((p) => (
                            <option key={p.planId} value={p.planId}>
                                {p.planName} ({p.planCode}) — ${p.priceMonthly}/mo
                            </option>
                        ))}
                    </select>
                </FormField>
                <FormField name="reason" label="Reason (optional)">
                    <textarea
                        id="reason"
                        value={reason}
                        onChange={(e) => setReason(e.target.value)}
                        rows={3}
                        placeholder="e.g. Support escalation, special arrangement…"
                        className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                </FormField>
                <div className="flex justify-end gap-2 pt-2">
                    <button type="button" onClick={onClose} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">
                        Cancel
                    </button>
                    <button type="submit" disabled={saving} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                        {saving ? 'Overriding…' : 'Override Plan'}
                    </button>
                </div>
            </form>
        </Modal>
    );
}
