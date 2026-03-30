import { Outlet } from 'react-router-dom';

export function AuthLayout() {
    return (
        <div className="flex min-h-screen items-center justify-center bg-background p-4">
            <div className="w-full max-w-md rounded-lg border border-border bg-card p-8 shadow-sm">
                <div className="mb-6 text-center">
                    <h1 className="text-2xl font-bold text-card-foreground">Nexus 2.0</h1>
                </div>
                <Outlet />
            </div>
        </div>
    );
}
