import { useState, useEffect } from 'react';
import { WifiOff } from 'lucide-react';

export function OfflineIndicator() {
    const [offline, setOffline] = useState(!navigator.onLine);

    useEffect(() => {
        const goOffline = () => setOffline(true);
        const goOnline = () => setOffline(false);

        window.addEventListener('offline', goOffline);
        window.addEventListener('online', goOnline);

        return () => {
            window.removeEventListener('offline', goOffline);
            window.removeEventListener('online', goOnline);
        };
    }, []);

    if (!offline) return null;

    return (
        <div className="fixed bottom-4 left-1/2 z-50 -translate-x-1/2 rounded-lg border border-yellow-300 bg-yellow-50 px-4 py-2.5 shadow-lg dark:border-yellow-700 dark:bg-yellow-900/30" role="alert">
            <div className="flex items-center gap-2 text-sm font-medium text-yellow-800 dark:text-yellow-200">
                <WifiOff size={16} />
                You're offline. Some features may not work until your connection is restored.
            </div>
        </div>
    );
}
