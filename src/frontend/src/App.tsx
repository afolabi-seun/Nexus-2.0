import { useEffect, useState } from 'react';
import { RouterProvider } from 'react-router-dom';
import { router } from '@/router';
import { ToastProvider } from '@/components/common/Toast';
import { useAuthStore, extractUserFromToken } from '@/stores/authStore';
import { useOrgStore } from '@/stores/orgStore';
import { securityApi } from '@/api/securityApi';
import { Loader2 } from 'lucide-react';

function SessionRestoreGate({ children }: { children: React.ReactNode }) {
  const refreshToken = useAuthStore((s) => s.refreshToken);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const login = useAuthStore((s) => s.login);
  const logout = useAuthStore((s) => s.logout);
  const refreshOrg = useOrgStore((s) => s.refresh);
  const [restoring, setRestoring] = useState(() => !!refreshToken && !isAuthenticated);

  useEffect(() => {
    if (!refreshToken || isAuthenticated) {
      setRestoring(false);
      return;
    }

    let cancelled = false;

    async function restore() {
      try {
        const response = await securityApi.refreshToken({ refreshToken: refreshToken! });
        if (cancelled) return;

        const user = extractUserFromToken(response.accessToken);
        login(
          { accessToken: response.accessToken, refreshToken: response.refreshToken },
          user
        );

        // Fetch org data for non-platform-admin users
        if (user.organizationId) {
          refreshOrg();
        }
      } catch {
        if (!cancelled) {
          logout();
        }
      } finally {
        if (!cancelled) {
          setRestoring(false);
        }
      }
    }

    restore();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (restoring) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return <>{children}</>;
}

function App() {
  return (
    <ToastProvider>
      <SessionRestoreGate>
        <RouterProvider router={router} />
      </SessionRestoreGate>
    </ToastProvider>
  );
}

export default App;
