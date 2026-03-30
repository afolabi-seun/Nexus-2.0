import {
    createContext,
    useContext,
    useState,
    useCallback,
    useEffect,
    type ReactNode,
} from 'react';
import { X, CheckCircle2, AlertCircle, Info } from 'lucide-react';

type ToastType = 'success' | 'error' | 'info';

interface Toast {
    id: string;
    type: ToastType;
    message: string;
}

interface ToastContextValue {
    addToast: (type: ToastType, message: string) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

let toastId = 0;

export function useToast() {
    const ctx = useContext(ToastContext);
    if (!ctx) throw new Error('useToast must be used within ToastProvider');
    return ctx;
}

function ToastItem({ toast, onDismiss }: { toast: Toast; onDismiss: (id: string) => void }) {
    useEffect(() => {
        if (toast.type !== 'error') {
            const timer = setTimeout(() => onDismiss(toast.id), 5000);
            return () => clearTimeout(timer);
        }
    }, [toast, onDismiss]);

    const icons: Record<ToastType, ReactNode> = {
        success: <CheckCircle2 size={16} className="text-green-500" />,
        error: <AlertCircle size={16} className="text-red-500" />,
        info: <Info size={16} className="text-blue-500" />,
    };

    const borderColors: Record<ToastType, string> = {
        success: 'border-l-green-500',
        error: 'border-l-red-500',
        info: 'border-l-blue-500',
    };

    return (
        <div
            className={`flex items-start gap-3 rounded-md border border-border border-l-4 ${borderColors[toast.type]} bg-popover p-3 shadow-lg`}
            role="alert"
        >
            <span className="mt-0.5">{icons[toast.type]}</span>
            <p className="flex-1 text-sm text-popover-foreground">{toast.message}</p>
            <button
                onClick={() => onDismiss(toast.id)}
                className="rounded p-0.5 text-muted-foreground hover:text-foreground"
                aria-label="Dismiss"
            >
                <X size={14} />
            </button>
        </div>
    );
}

export function ToastProvider({ children }: { children: ReactNode }) {
    const [toasts, setToasts] = useState<Toast[]>([]);

    const addToast = useCallback((type: ToastType, message: string) => {
        const id = String(++toastId);
        setToasts((prev) => [...prev, { id, type, message }]);
    }, []);

    const dismiss = useCallback((id: string) => {
        setToasts((prev) => prev.filter((t) => t.id !== id));
    }, []);

    return (
        <ToastContext.Provider value={{ addToast }}>
            {children}
            <div className="fixed right-4 top-4 z-[100] flex w-80 flex-col gap-2">
                {toasts.map((t) => (
                    <ToastItem key={t.id} toast={t} onDismiss={dismiss} />
                ))}
            </div>
        </ToastContext.Provider>
    );
}
