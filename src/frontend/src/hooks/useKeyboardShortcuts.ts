import { useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';

export interface Shortcut {
    key: string;
    label: string;
    description: string;
    action: () => void;
    meta?: boolean;
}

export function useKeyboardShortcuts(shortcuts: Shortcut[]) {
    const handler = useCallback((e: KeyboardEvent) => {
        // Don't trigger when typing in inputs
        const target = e.target as HTMLElement;
        if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.tagName === 'SELECT' || target.isContentEditable) {
            return;
        }

        for (const shortcut of shortcuts) {
            const metaMatch = shortcut.meta ? (e.metaKey || e.ctrlKey) : !(e.metaKey || e.ctrlKey);
            if (e.key.toLowerCase() === shortcut.key.toLowerCase() && metaMatch && !e.altKey) {
                e.preventDefault();
                shortcut.action();
                return;
            }
        }
    }, [shortcuts]);

    useEffect(() => {
        document.addEventListener('keydown', handler);
        return () => document.removeEventListener('keydown', handler);
    }, [handler]);
}

export function useAppShortcuts() {
    const navigate = useNavigate();

    const shortcuts: Shortcut[] = [
        { key: 'g', label: 'g then d', description: 'Go to Dashboard', action: () => navigate('/') },
        { key: 'p', label: 'g then p', description: 'Go to Projects', action: () => navigate('/projects') },
        { key: 's', label: 'g then s', description: 'Go to Stories', action: () => navigate('/stories') },
        { key: 'b', label: 'g then b', description: 'Go to Kanban Board', action: () => navigate('/boards/kanban') },
        { key: 'r', label: 'g then r', description: 'Go to Sprints', action: () => navigate('/sprints') },
        { key: 'm', label: 'g then m', description: 'Go to Members', action: () => navigate('/members') },
        { key: 't', label: 'g then t', description: 'Go to Time Tracking', action: () => navigate('/time-tracking') },
        { key: 'a', label: 'g then a', description: 'Go to Analytics', action: () => navigate('/analytics') },
        { key: '/', label: '/', description: 'Focus search', action: () => {
            const searchInput = document.querySelector<HTMLInputElement>('input[placeholder*="Search"]');
            searchInput?.focus();
        }},
        { key: '?', label: '?', description: 'Show keyboard shortcuts', action: () => {
            document.dispatchEvent(new CustomEvent('toggle-shortcuts-dialog'));
        }},
    ];

    useKeyboardShortcuts(shortcuts);

    return shortcuts;
}
