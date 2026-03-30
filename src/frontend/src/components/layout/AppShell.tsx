import { useState, useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar.js';
import { Header } from './Header.js';

const SIDEBAR_KEY = 'nexus_sidebar_collapsed';

function loadCollapsed(): boolean {
    try {
        return localStorage.getItem(SIDEBAR_KEY) === 'true';
    } catch {
        return false;
    }
}

export function AppShell() {
    const [collapsed, setCollapsed] = useState(loadCollapsed);

    useEffect(() => {
        try {
            localStorage.setItem(SIDEBAR_KEY, String(collapsed));
        } catch {
            // ignore
        }
    }, [collapsed]);

    const toggle = () => setCollapsed((c) => !c);

    return (
        <div className="flex h-screen overflow-hidden bg-background">
            <aside
                className={`flex-shrink-0 border-r border-border transition-all duration-200 ${collapsed ? 'w-16' : 'w-60'
                    }`}
            >
                <Sidebar collapsed={collapsed} onToggle={toggle} />
            </aside>

            <div className="flex flex-1 flex-col overflow-hidden">
                <header className="flex-shrink-0 border-b border-border">
                    <Header />
                </header>

                <main className="flex-1 overflow-auto p-6">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}
