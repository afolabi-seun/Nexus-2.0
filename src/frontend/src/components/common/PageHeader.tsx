import { useState, useEffect } from 'react';
import { X } from 'lucide-react';

interface PageHeaderProps {
    title: string;
    description?: string;
    dismissKey?: string;
    children?: React.ReactNode;
}

function isDismissed(key: string): boolean {
    try { return localStorage.getItem(`nexus_help_${key}`) === '1'; } catch { return false; }
}

function dismiss(key: string) {
    try { localStorage.setItem(`nexus_help_${key}`, '1'); } catch { /* ignore */ }
}

export function PageHeader({ title, description, dismissKey, children }: PageHeaderProps) {
    const [showDesc, setShowDesc] = useState(true);

    useEffect(() => {
        if (dismissKey && isDismissed(dismissKey)) setShowDesc(false);
    }, [dismissKey]);

    const handleDismiss = () => {
        if (dismissKey) dismiss(dismissKey);
        setShowDesc(false);
    };

    return (
        <div className="flex items-start justify-between">
            <div className="space-y-1">
                <h1 className="text-2xl font-semibold text-foreground">{title}</h1>
                {description && showDesc && (
                    <div className="flex items-center gap-2">
                        <p className="text-sm text-muted-foreground">{description}</p>
                        {dismissKey && (
                            <button
                                onClick={handleDismiss}
                                className="shrink-0 rounded p-0.5 text-muted-foreground hover:text-foreground"
                                aria-label="Dismiss help text"
                            >
                                <X size={14} />
                            </button>
                        )}
                    </div>
                )}
            </div>
            {children && <div className="flex items-center gap-2 shrink-0">{children}</div>}
        </div>
    );
}
