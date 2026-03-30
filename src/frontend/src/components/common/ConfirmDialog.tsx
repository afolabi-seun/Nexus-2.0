import { Modal } from './Modal.js';

interface ConfirmDialogProps {
    open: boolean;
    onConfirm: () => void;
    onCancel: () => void;
    title: string;
    message: string;
    confirmLabel?: string;
    destructive?: boolean;
}

export function ConfirmDialog({
    open,
    onConfirm,
    onCancel,
    title,
    message,
    confirmLabel = 'Confirm',
    destructive = true,
}: ConfirmDialogProps) {
    return (
        <Modal open={open} onClose={onCancel} title={title}>
            <p className="mb-6 text-sm text-muted-foreground">{message}</p>
            <div className="flex justify-end gap-3">
                <button
                    onClick={onCancel}
                    className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent"
                >
                    Cancel
                </button>
                <button
                    onClick={onConfirm}
                    className={`rounded-md px-4 py-2 text-sm font-medium text-white ${destructive
                        ? 'bg-destructive hover:bg-destructive/90'
                        : 'bg-primary hover:bg-primary/90'
                        }`}
                >
                    {confirmLabel}
                </button>
            </div>
        </Modal>
    );
}
