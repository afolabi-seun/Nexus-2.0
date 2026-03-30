import { useEffect, useRef, type ReactNode } from 'react';
import { X } from 'lucide-react';

interface ModalProps {
    open: boolean;
    onClose: () => void;
    title: string;
    children: ReactNode;
}

export function Modal({ open, onClose, title, children }: ModalProps) {
    const dialogRef = useRef<HTMLDialogElement>(null);
    const contentRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const dialog = dialogRef.current;
        if (!dialog) return;

        if (open) {
            dialog.showModal();
            // Focus trap: focus first focusable element
            const focusable = contentRef.current?.querySelector<HTMLElement>(
                'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
            );
            focusable?.focus();
        } else {
            dialog.close();
        }
    }, [open]);

    useEffect(() => {
        const dialog = dialogRef.current;
        if (!dialog) return;
        const handleCancel = (e: Event) => {
            e.preventDefault();
            onClose();
        };
        dialog.addEventListener('cancel', handleCancel);
        return () => dialog.removeEventListener('cancel', handleCancel);
    }, [onClose]);

    if (!open) return null;

    return (
        <dialog
            ref={dialogRef}
            className="fixed inset-0 z-50 m-0 h-full w-full max-h-full max-w-full border-none bg-transparent p-0 backdrop:bg-black/50"
            onClick={(e) => {
                if (e.target === dialogRef.current) onClose();
            }}
        >
            <div className="flex min-h-full items-center justify-center p-4">
                <div
                    ref={contentRef}
                    className="w-full max-w-lg rounded-lg border border-border bg-card p-6 shadow-lg"
                    onClick={(e) => e.stopPropagation()}
                >
                    <div className="mb-4 flex items-center justify-between">
                        <h2 className="text-lg font-semibold text-card-foreground">{title}</h2>
                        <button
                            onClick={onClose}
                            className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                            aria-label="Close"
                        >
                            <X size={18} />
                        </button>
                    </div>
                    {children}
                </div>
            </div>
        </dialog>
    );
}
