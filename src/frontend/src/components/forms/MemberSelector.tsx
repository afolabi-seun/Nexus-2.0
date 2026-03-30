import { useState, useEffect, useRef } from 'react';
import { Search } from 'lucide-react';
import { profileApi } from '@/api/profileApi';
import type { TeamMember } from '@/types/profile';
import { useDebounce } from '@/hooks/useDebounce';

interface MemberSelectorProps {
    value?: string;
    onSelect: (memberId: string, member: TeamMember) => void;
    departmentId?: string;
    placeholder?: string;
}

export function MemberSelector({
    value,
    onSelect,
    departmentId,
    placeholder = 'Search members...',
}: MemberSelectorProps) {
    const [query, setQuery] = useState('');
    const [members, setMembers] = useState<TeamMember[]>([]);
    const [open, setOpen] = useState(false);
    const [loading, setLoading] = useState(false);
    const [selectedName, setSelectedName] = useState('');
    const debouncedQuery = useDebounce(query, 300);
    const containerRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (!debouncedQuery.trim()) {
            setMembers([]);
            return;
        }
        let cancelled = false;
        setLoading(true);
        profileApi
            .getTeamMembers({ page: 1, pageSize: 10, departmentId })
            .then((res) => {
                if (!cancelled) {
                    const filtered = res.data.filter((m) =>
                        `${m.firstName} ${m.lastName}`.toLowerCase().includes(debouncedQuery.toLowerCase())
                    );
                    setMembers(filtered);
                }
            })
            .catch(() => {
                if (!cancelled) setMembers([]);
            })
            .finally(() => {
                if (!cancelled) setLoading(false);
            });
        return () => { cancelled = true; };
    }, [debouncedQuery, departmentId]);

    useEffect(() => {
        function handleClickOutside(e: MouseEvent) {
            if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
                setOpen(false);
            }
        }
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    return (
        <div className="relative" ref={containerRef}>
            <div className="relative">
                <Search size={14} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <input
                    type="text"
                    value={open ? query : selectedName || query}
                    onChange={(e) => { setQuery(e.target.value); setOpen(true); }}
                    onFocus={() => setOpen(true)}
                    placeholder={placeholder}
                    className="h-9 w-full rounded-md border border-input bg-background pl-8 pr-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
            </div>
            {open && (query.trim() || members.length > 0) && (
                <ul className="absolute z-50 mt-1 max-h-48 w-full overflow-auto rounded-md border border-border bg-popover py-1 shadow-lg">
                    {loading && (
                        <li className="px-3 py-2 text-sm text-muted-foreground">Searching...</li>
                    )}
                    {!loading && members.length === 0 && query.trim() && (
                        <li className="px-3 py-2 text-sm text-muted-foreground">No members found</li>
                    )}
                    {members.map((m) => (
                        <li key={m.teamMemberId}>
                            <button
                                type="button"
                                onClick={() => {
                                    onSelect(m.teamMemberId, m);
                                    setSelectedName(`${m.firstName} ${m.lastName}`);
                                    setQuery('');
                                    setOpen(false);
                                }}
                                className={`flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-accent ${value === m.teamMemberId ? 'bg-accent' : ''
                                    }`}
                            >
                                <span className="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-[10px] font-medium text-primary-foreground">
                                    {m.firstName.charAt(0)}{m.lastName.charAt(0)}
                                </span>
                                <span className="text-popover-foreground">{m.firstName} {m.lastName}</span>
                            </button>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
