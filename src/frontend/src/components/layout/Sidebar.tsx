import { useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import {
    LayoutDashboard,
    FolderKanban,
    BookOpen,
    Columns3,
    Timer,
    Users,
    Building2,
    BarChart3,
    Settings,
    Mail,
    ChevronLeft,
    ChevronRight,
    ChevronDown,
    ChevronUp,
    Kanban,
    CalendarDays,
    Archive,
    CreditCard,
    Clock,
    TrendingUp,
    Bell,
    ClipboardList,
    type LucideIcon,
} from 'lucide-react';
import { useOrgStore } from '@/stores/orgStore';
import { useAuth } from '@/hooks/useAuth';
import type { NavigationItem as NavItemType } from '@/types/profile';

interface SidebarProps {
    collapsed: boolean;
    onToggle: () => void;
}

// Icon registry — maps icon name strings from the DB to lucide-react components
const iconMap: Record<string, LucideIcon> = {
    LayoutDashboard, FolderKanban, BookOpen, Columns3, Timer, Users,
    Building2, BarChart3, Settings, Mail, Kanban, CalendarDays, Archive,
    CreditCard, Clock, TrendingUp, Bell, ClipboardList,
};

interface NavSection {
    label: string;
    minPermissionLevel: number;
    items: NavItemType[];
}

// Fallback navigation used when the API is unreachable.
// Once the backend responds, these are replaced by DB-driven items.
const fallbackSections: NavSection[] = [
    {
        label: 'Work',
        minPermissionLevel: 25,
        items: [
            { navigationItemId: 'f-1', label: 'Dashboard', path: '/', icon: 'LayoutDashboard', sortOrder: 1, parentId: null, minPermissionLevel: 25, isEnabled: true, children: [] },
            { navigationItemId: 'f-2', label: 'Projects', path: '/projects', icon: 'FolderKanban', sortOrder: 2, parentId: null, minPermissionLevel: 25, isEnabled: true, children: [] },
            { navigationItemId: 'f-3', label: 'Stories', path: '/stories', icon: 'BookOpen', sortOrder: 3, parentId: null, minPermissionLevel: 25, isEnabled: true, children: [] },
            {
                navigationItemId: 'f-4', label: 'Boards', path: '/boards', icon: 'Columns3', sortOrder: 4, parentId: null, minPermissionLevel: 25, isEnabled: true,
                children: [
                    { navigationItemId: 'f-4a', label: 'Kanban', path: '/boards/kanban', icon: 'Kanban', sortOrder: 1, parentId: 'f-4', minPermissionLevel: 25, isEnabled: true, children: [] },
                    { navigationItemId: 'f-4b', label: 'Sprint Board', path: '/boards/sprint', icon: 'CalendarDays', sortOrder: 2, parentId: 'f-4', minPermissionLevel: 25, isEnabled: true, children: [] },
                    { navigationItemId: 'f-4c', label: 'Dept Board', path: '/boards/department', icon: 'Building2', sortOrder: 3, parentId: 'f-4', minPermissionLevel: 25, isEnabled: true, children: [] },
                    { navigationItemId: 'f-4d', label: 'Backlog', path: '/boards/backlog', icon: 'Archive', sortOrder: 4, parentId: 'f-4', minPermissionLevel: 25, isEnabled: true, children: [] },
                ],
            },
            { navigationItemId: 'f-5', label: 'Sprints', path: '/sprints', icon: 'Timer', sortOrder: 5, parentId: null, minPermissionLevel: 25, isEnabled: true, children: [] },
        ],
    },
    {
        label: 'Tracking',
        minPermissionLevel: 25,
        items: [
            { navigationItemId: 'f-6', label: 'Time Tracking', path: '/time-tracking', icon: 'Clock', sortOrder: 1, parentId: null, minPermissionLevel: 50, isEnabled: true, children: [] },
            { navigationItemId: 'f-7', label: 'Analytics', path: '/analytics', icon: 'TrendingUp', sortOrder: 2, parentId: null, minPermissionLevel: 25, isEnabled: true, children: [] },
            { navigationItemId: 'f-8', label: 'Reports', path: '/reports', icon: 'BarChart3', sortOrder: 3, parentId: null, minPermissionLevel: 25, isEnabled: true, children: [] },
        ],
    },
    {
        label: 'Team',
        minPermissionLevel: 25,
        items: [
            { navigationItemId: 'f-9', label: 'Members', path: '/members', icon: 'Users', sortOrder: 1, parentId: null, minPermissionLevel: 25, isEnabled: true, children: [] },
            { navigationItemId: 'f-10', label: 'Departments', path: '/departments', icon: 'Building2', sortOrder: 2, parentId: null, minPermissionLevel: 25, isEnabled: true, children: [] },
            { navigationItemId: 'f-11', label: 'Invites', path: '/invites', icon: 'Mail', sortOrder: 3, parentId: null, minPermissionLevel: 75, isEnabled: true, children: [] },
        ],
    },
    {
        label: 'Organization',
        minPermissionLevel: 75,
        items: [
            { navigationItemId: 'f-12', label: 'Settings', path: '/settings', icon: 'Settings', sortOrder: 1, parentId: null, minPermissionLevel: 100, isEnabled: true, children: [] },
            { navigationItemId: 'f-13', label: 'Billing', path: '/billing', icon: 'CreditCard', sortOrder: 2, parentId: null, minPermissionLevel: 100, isEnabled: true, children: [] },
            { navigationItemId: 'f-14', label: 'Audit Logs', path: '/audit-logs', icon: 'ClipboardList', sortOrder: 3, parentId: null, minPermissionLevel: 100, isEnabled: true, children: [] },
            { navigationItemId: 'f-15', label: 'Notifications', path: '/notifications', icon: 'Bell', sortOrder: 4, parentId: null, minPermissionLevel: 75, isEnabled: true, children: [] },
        ],
    },
];

const rolePermissionLevel: Record<string, number> = {
    OrgAdmin: 100, DeptLead: 75, Member: 50, Viewer: 25,
};

function getIcon(iconName: string, size: number = 20): React.ReactNode {
    const Icon = iconMap[iconName];
    return Icon ? <Icon size={size} /> : <LayoutDashboard size={size} />;
}

function isActiveRoute(currentPath: string, itemPath: string): boolean {
    if (itemPath === '/') return currentPath === '/';
    return currentPath.startsWith(itemPath);
}

function filterByPermission(items: NavItemType[], permLevel: number): NavItemType[] {
    return items
        .filter((i) => i.isEnabled && i.minPermissionLevel <= permLevel)
        .map((i) => ({
            ...i,
            children: i.children
                ? i.children.filter((c) => c.isEnabled && c.minPermissionLevel <= permLevel)
                : [],
        }));
}

function buildSections(dbNavigation: NavItemType[], navigationLoaded: boolean, permLevel: number): NavSection[] {
    // If DB navigation is loaded and has items, use flat list in a single section (DB-driven)
    if (navigationLoaded && dbNavigation.length > 0) {
        const filtered = filterByPermission(dbNavigation, permLevel);
        return [{ label: '', minPermissionLevel: 25, items: filtered }];
    }

    // Otherwise use the structured fallback sections
    return fallbackSections
        .filter((s) => s.minPermissionLevel <= permLevel)
        .map((s) => ({
            ...s,
            items: filterByPermission(s.items, permLevel),
        }))
        .filter((s) => s.items.length > 0);
}

export function Sidebar({ collapsed, onToggle }: SidebarProps) {
    const location = useLocation();
    const { user } = useAuth();
    const dbNavigation = useOrgStore((s) => s.navigation);
    const navigationLoaded = useOrgStore((s) => s.navigationLoaded);
    const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>({});

    const toggleGroup = (label: string) => {
        setExpandedGroups((prev) => ({ ...prev, [label]: !prev[label] }));
    };

    const permLevel = rolePermissionLevel[user?.roleName ?? 'Viewer'] ?? 25;
    const sections = buildSections(dbNavigation, navigationLoaded, permLevel);

    return (
        <nav className="flex h-full flex-col bg-card" aria-label="Main navigation">
            <div className="flex h-14 items-center justify-between border-b border-border px-3">
                {!collapsed && (
                    <span className="text-lg font-semibold text-foreground">Nexus</span>
                )}
                <button
                    onClick={onToggle}
                    className="rounded-md p-1.5 text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                    aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
                >
                    {collapsed ? <ChevronRight size={18} /> : <ChevronLeft size={18} />}
                </button>
            </div>

            <div className="flex-1 overflow-y-auto px-2 py-3">
                {sections.map((section, sectionIdx) => (
                    <div key={section.label || sectionIdx}>
                        {sectionIdx > 0 && (
                            <div className="my-3 border-t border-border" />
                        )}
                        {!collapsed && section.label && (
                            <p className="mb-1 px-3 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                                {section.label}
                            </p>
                        )}
                        <ul className="space-y-1">
                            {section.items.map((item) => {
                                const active = isActiveRoute(location.pathname, item.path);
                                const hasChildren = item.children && item.children.length > 0;
                                const expanded = expandedGroups[item.label] ?? active;

                                if (hasChildren) {
                                    return (
                                        <li key={item.navigationItemId}>
                                            <button
                                                onClick={() => toggleGroup(item.label)}
                                                className={`flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${active ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
                                                    } ${collapsed ? 'justify-center' : ''}`}
                                                title={collapsed ? item.label : undefined}
                                            >
                                                {getIcon(item.icon)}
                                                {!collapsed && (
                                                    <>
                                                        <span className="flex-1 text-left">{item.label}</span>
                                                        {expanded ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                                                    </>
                                                )}
                                            </button>
                                            {!collapsed && expanded && (
                                                <ul className="ml-5 mt-1 space-y-1">
                                                    {item.children.map((child) => (
                                                        <li key={child.navigationItemId}>
                                                            <NavLink
                                                                to={child.path}
                                                                className={({ isActive }) =>
                                                                    `flex items-center gap-2 rounded-md px-3 py-1.5 text-sm transition-colors ${isActive ? 'bg-primary/10 text-primary font-medium' : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
                                                                    }`
                                                                }
                                                            >
                                                                {getIcon(child.icon, 18)}
                                                                <span>{child.label}</span>
                                                            </NavLink>
                                                        </li>
                                                    ))}
                                                </ul>
                                            )}
                                        </li>
                                    );
                                }

                                return (
                                    <li key={item.navigationItemId}>
                                        <NavLink
                                            to={item.path}
                                            end={item.path === '/'}
                                            className={({ isActive }) =>
                                                `flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
                                                } ${collapsed ? 'justify-center' : ''}`
                                            }
                                            title={collapsed ? item.label : undefined}
                                        >
                                            {getIcon(item.icon)}
                                            {!collapsed && <span>{item.label}</span>}
                                        </NavLink>
                                    </li>
                                );
                            })}
                        </ul>
                    </div>
                ))}
            </div>
        </nav>
    );
}
