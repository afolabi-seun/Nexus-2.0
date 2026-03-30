import { useState, useEffect } from 'react';
import { profileApi } from '@/api/profileApi';
import type { TeamMember } from '@/types/profile';

interface MentionAutocompleteProps {
    query: string;
    onSelect: (name: string) => void;
}

export function MentionAutocomplete({ query, onSelect }: MentionAutocompleteProps) {
    const [members, setMembers] = useState<TeamMember[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        let cancelled = false;
        setLoading(true);
        profileApi
            .getTeamMembers({ page: 1, pageSize: 20 })
            .then((res) => {
                if (cancelled) return;
                const filtered = query
                    ? res.data.filter((m) =>
                        `${m.firstName} ${m.lastName}`.toLowerCase().includes(query.toLowerCase())
                    )
                    : res.data;
                setMembers(filtered.slice(0, 8));
            })
            .catch(() => {
                if (!cancelled) setMembers([]);
            })
            .finally(() => {
                if (!cancelled) setLoading(false);
            });
        return () => { cancelled = true; };
    }, [query]);

    if (loading && members.length === 0) {
        return (
            <div className="absolute z-50 mt-1 w-64 rounded-md border border-border bg-popover py-1 shadow-lg">
                <p className="px-3 py-2 text-xs text-muted-foreground">Searching...</p>
            </div>
        );
    }

    if (members.length === 0) return null;

    return (
        <ul className="absolute z-50 mt-1 max-h-48 w-64 overflow-auto rounded-md border border-border bg-popover py-1 shadow-lg">
            {members.map((m) => (
                <li key={m.teamMemberId}>
                    <button
                        type="button"
                        onClick={() => onSelect(`${m.firstName} ${m.lastName}`)}
                        className="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-accent"
                    >
                        <span className="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-[10px] font-medium text-primary-foreground">
                            {m.firstName.charAt(0)}{m.lastName.charAt(0)}
                        </span>
                        <span className="text-popover-foreground">{m.firstName} {m.lastName}</span>
                    </button>
                </li>
            ))}
        </ul>
    );
}
