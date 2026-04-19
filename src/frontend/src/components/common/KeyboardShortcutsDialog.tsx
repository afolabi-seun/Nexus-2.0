import { useState, useEffect } from 'react';
import { Modal } from '@/components/common/Modal';

interface ShortcutEntry {
    key: string;
    description: string;
}

const SHORTCUTS: { section: string; items: ShortcutEntry[] }[] = [
    {
        section: 'Navigation',
        items: [
            { key: 'g d', description: 'Go to Dashboard' },
            { key: 'g p', description: 'Go to Projects' },
            { key: 'g s', description: 'Go to Stories' },
            { key: 'g b', description: 'Go to Kanban Board' },
            { key: 'g r', description: 'Go to Sprints' },
            { key: 'g m', description: 'Go to Members' },
            { key: 'g t', description: 'Go to Time Tracking' },
            { key: 'g a', description: 'Go to Analytics' },
        ],
    },
    {
        section: 'Actions',
        items: [
            { key: '/', description: 'Focus search bar' },
            { key: '?', description: 'Show this dialog' },
        ],
    },
];

export function KeyboardShortcutsDialog() {
    const [open, setOpen] = useState(false);

    useEffect(() => {
        const handler = () => setOpen((o) => !o);
        document.addEventListener('toggle-shortcuts-dialog', handler);
        return () => document.removeEventListener('toggle-shortcuts-dialog', handler);
    }, []);

    return (
        <Modal open={open} onClose={() => setOpen(false)} title="Keyboard Shortcuts">
            <div className="space-y-4">
                {SHORTCUTS.map((group) => (
                    <div key={group.section} className="space-y-2">
                        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">{group.section}</h3>
                        <div className="space-y-1">
                            {group.items.map((item) => (
                                <div key={item.key} className="flex items-center justify-between py-1">
                                    <span className="text-sm text-foreground">{item.description}</span>
                                    <div className="flex items-center gap-1">
                                        {item.key.split(' ').map((k, i) => (
                                            <span key={i}>
                                                {i > 0 && <span className="mx-0.5 text-xs text-muted-foreground">then</span>}
                                                <kbd className="inline-flex h-6 min-w-[24px] items-center justify-center rounded border border-border bg-muted px-1.5 text-xs font-medium text-muted-foreground">
                                                    {k}
                                                </kbd>
                                            </span>
                                        ))}
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                ))}
            </div>
        </Modal>
    );
}
