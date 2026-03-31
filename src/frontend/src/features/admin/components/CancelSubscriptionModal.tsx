import { useState, useEffect } from 'react';
import { Modal } from '@/components/common/Modal';
import { FormField } from '@/components/forms/FormField';

interface CancelSubscriptionModalProps {
    open: boolean;
    onClose: () => void;
    onSubmit: (reason: string) => Promise<void>;
    organizationName?: string;
}

export function CancelSubscriptionModal({ open, onClose, onSubmit, organizationName }: CancelSubscriptionModalProps) {
    const [reason, setReason] = useState('');
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (open) {
            setReason('');
            setError(null);
        }
    }, [open]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!reason.trim()) {
            setError('Reason is required');
            return;
        }
        setSaving(true);
        setError(null);
        try {
            await onSubmit(reason);
            onClose();
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to cancel subscription');
        } finally {
            setSaving(false);
        }
    };

    return (
        <Modal open={open} onClose={onClose} title="Cancel Subscription">
            <form onSubmit={handleSubmit} className="space-y-4">
                <p className="text-sm text-muted-foreground">
                    This will immediately cancel the subscription{organizationName ? ` for ${organizationName}` : ''}. This action cannot be undone.
                </p>
                {error && (
                    <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive" role="alert">
                        {error}
                    </p>
                )}
                <FormField name="reason" label="Cancellation Reason" required error={!reason.trim() && error ? 'Reason is required' : undefined}>
                    <textarea
                        id="reason"
                        value={reason}
                        onChange={(e) => setReason(e.target.value)}
                        rows={3}
                        placeholder="Provide a reason for cancellation…"
                        className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                </FormField>
                <div className="flex justify-end gap-2 pt-2">
                    <button type="button" onClick={onClose} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">
                        Keep Subscription
                    </button>
                    <button type="submit" disabled={saving} className="rounded-md bg-destructive px-4 py-2 text-sm font-medium text-destructive-foreground hover:bg-destructive/90 disabled:opacity-50">
                        {saving ? 'Cancelling…' : 'Cancel Subscription'}
                    </button>
                </div>
            </form>
        </Modal>
    );
}
