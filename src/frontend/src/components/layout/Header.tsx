import { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, LogOut, User, Settings, Monitor, ChevronDown } from 'lucide-react';
import { useAuth } from '@/hooks/useAuth';
import { useOrg } from '@/hooks/useOrg';
import { securityApi } from '@/api/securityApi';
import { NotificationBellDropdown } from '@/features/notifications/components/NotificationBellDropdown';

export function Header() {
    const { user, logout: clearAuth } = useAuth();
    const { organization } = useOrg();
    const navigate = useNavigate();
    const [dropdownOpen, setDropdownOpen] = useState(false);
    const [searchValue, setSearchValue] = useState('');
    const dropdownRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        function handleClickOutside(e: MouseEvent) {
            if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
                setDropdownOpen(false);
            }
        }
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const handleLogout = async () => {
        try {
            await securityApi.logout();
        } catch {
            // proceed with local logout even if API fails
        }
        clearAuth();
        navigate('/login', { replace: true });
    };

    const handleSearch = (e: React.FormEvent) => {
        e.preventDefault();
        if (searchValue.trim()) {
            navigate(`/search?q=${encodeURIComponent(searchValue.trim())}`);
            setSearchValue('');
        }
    };

    const initials = user
        ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase()
        : '??';

    return (
        <div className="flex h-14 items-center justify-between px-4">
            <div className="flex items-center gap-3">
                {organization && (
                    <span className="text-sm font-semibold text-foreground">
                        {organization.name}
                    </span>
                )}
            </div>

            <div className="flex items-center gap-3">
                <form onSubmit={handleSearch} className="relative">
                    <Search size={16} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
                    <input
                        type="text"
                        placeholder="Search..."
                        value={searchValue}
                        onChange={(e) => setSearchValue(e.target.value)}
                        className="h-8 w-56 rounded-md border border-input bg-background pl-8 pr-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                </form>

                <NotificationBellDropdown />

                <div className="relative" ref={dropdownRef}>
                    <button
                        onClick={() => setDropdownOpen((o) => !o)}
                        className="flex items-center gap-2 rounded-md p-1.5 text-sm hover:bg-accent"
                    >
                        <span className="flex h-7 w-7 items-center justify-center rounded-full bg-primary text-xs font-medium text-primary-foreground">
                            {initials}
                        </span>
                        {!dropdownOpen ? <ChevronDown size={14} className="text-muted-foreground" /> : null}
                    </button>

                    {dropdownOpen && (
                        <div className="absolute right-0 top-full z-50 mt-1 w-48 rounded-md border border-border bg-popover py-1 shadow-lg">
                            <div className="border-b border-border px-3 py-2">
                                <p className="text-sm font-medium text-popover-foreground">
                                    {user?.firstName} {user?.lastName}
                                </p>
                                <p className="text-xs text-muted-foreground">{user?.email}</p>
                            </div>
                            <button
                                onClick={() => { setDropdownOpen(false); navigate('/members/' + user?.userId); }}
                                className="flex w-full items-center gap-2 px-3 py-2 text-sm text-popover-foreground hover:bg-accent"
                            >
                                <User size={14} /> Profile
                            </button>
                            <button
                                onClick={() => { setDropdownOpen(false); navigate('/preferences'); }}
                                className="flex w-full items-center gap-2 px-3 py-2 text-sm text-popover-foreground hover:bg-accent"
                            >
                                <Settings size={14} /> Preferences
                            </button>
                            <button
                                onClick={() => { setDropdownOpen(false); navigate('/sessions'); }}
                                className="flex w-full items-center gap-2 px-3 py-2 text-sm text-popover-foreground hover:bg-accent"
                            >
                                <Monitor size={14} /> Sessions
                            </button>
                            <div className="border-t border-border">
                                <button
                                    onClick={handleLogout}
                                    className="flex w-full items-center gap-2 px-3 py-2 text-sm text-destructive hover:bg-accent"
                                >
                                    <LogOut size={14} /> Logout
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
