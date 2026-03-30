import { useState } from 'react';
import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { Building2, LogOut, ChevronLeft, ChevronRight } from 'lucide-react';
import { useAuth } from '@/hooks/useAuth';
import { securityApi } from '@/api/securityApi';

export function AdminLayout() {
    const { user, logout: clearAuth } = useAuth();
    const navigate = useNavigate();
    const [collapsed, setCollapsed] = useState(false);

    const handleLogout = async () => {
        try {
            await securityApi.logout();
        } catch {
            // proceed
        }
        clearAuth();
        navigate('/login', { replace: true });
    };

    const initials = user
        ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase()
        : '??';

    return (
        <div className="flex h-screen overflow-hidden bg-background">
            <aside
                className={`flex-shrink-0 border-r border-border transition-all duration-200 ${collapsed ? 'w-16' : 'w-60'
                    }`}
            >
                <nav className="flex h-full flex-col bg-card" aria-label="Admin navigation">
                    <div className="flex h-14 items-center justify-between border-b border-border px-3">
                        {!collapsed && (
                            <span className="text-lg font-semibold text-foreground">Admin</span>
                        )}
                        <button
                            onClick={() => setCollapsed((c) => !c)}
                            className="rounded-md p-1.5 text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                            aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
                        >
                            {collapsed ? <ChevronRight size={18} /> : <ChevronLeft size={18} />}
                        </button>
                    </div>

                    <ul className="flex-1 space-y-1 px-2 py-3">
                        <li>
                            <NavLink
                                to="/admin/organizations"
                                className={({ isActive }) =>
                                    `flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${isActive
                                        ? 'bg-primary/10 text-primary'
                                        : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
                                    } ${collapsed ? 'justify-center' : ''}`
                                }
                                title={collapsed ? 'Organizations' : undefined}
                            >
                                <Building2 size={20} />
                                {!collapsed && <span>Organizations</span>}
                            </NavLink>
                        </li>
                    </ul>
                </nav>
            </aside>

            <div className="flex flex-1 flex-col overflow-hidden">
                <header className="flex h-14 flex-shrink-0 items-center justify-between border-b border-border px-4">
                    <span className="text-sm font-semibold text-foreground">Platform Administration</span>
                    <div className="flex items-center gap-3">
                        <span className="flex h-7 w-7 items-center justify-center rounded-full bg-primary text-xs font-medium text-primary-foreground">
                            {initials}
                        </span>
                        <button
                            onClick={handleLogout}
                            className="flex items-center gap-1.5 rounded-md px-2 py-1.5 text-sm text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                        >
                            <LogOut size={14} /> Logout
                        </button>
                    </div>
                </header>

                <main className="flex-1 overflow-auto p-6">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}
