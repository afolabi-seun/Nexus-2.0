import { useState, useRef, useEffect } from 'react';
import { HelpCircle } from 'lucide-react';

interface HelpTooltipProps {
    text: string;
}

export function HelpTooltip({ text }: HelpTooltipProps) {
    const [open, setOpen] = useState(false);
    const ref = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (!open) return;
        function handleClick(e: MouseEvent) {
            if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
        }
        document.addEventListener('mousedown', handleClick);
        return () => document.removeEventListener('mousedown', handleClick);
    }, [open]);

    return (
        <div className="relative inline-flex" ref={ref}>
            <button
                type="button"
                onClick={() => setOpen((o) => !o)}
                className="text-muted-foreground hover:text-foreground"
                aria-label="Help"
            >
                <HelpCircle size={14} />
            </button>
            {open && (
                <div className="absolute bottom-full left-1/2 z-50 mb-2 w-56 -translate-x-1/2 rounded-md border border-border bg-popover p-2.5 text-xs text-popover-foreground shadow-md">
                    {text}
                </div>
            )}
        </div>
    );
}
