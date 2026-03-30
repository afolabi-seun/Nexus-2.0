import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { utilityApi } from '@/api/utilityApi';
import type { NotificationLog } from '@/types/utility';
import {
    Bell,
    Mail,
    Smartphone,
    Monitor,
    CheckCircle2,
    Clock,
    AlertCircle,
} from 'lucide-react';

const channelIcons: Record<string, React.ReactNode> = {
    Email: <Mail size={12} />,
    Push: <Smartphone size={12} />,
    InApp: <Monitor size={12} />,
};

const statusIcons: Record<string, React.ReactNode> = {
    Sent: <CheckCircle2 size={12} className="text-green-500" />,
    Pending: <Clock size={12} className="text-yellow-500" />,
    Failed: <AlertCircle size={12} className="text-red-500" />,
};

export function NotificationBellDropdown() {
    const navigate = useNavigate();
    const [open, setOpen] = useState(false);
    const [notifications, setNotifications] = useState<NotificationLog[]>([]);
    const [unreadCount, setUnreadCount] = useState(0);
    const ref = useRef<HTMLDivElement>(null);

    useEffect(() => {
        utilityApi
            .getNotificationLogs({ page: 1, pageSize: 5 })
            .then((result) => {
                setNotifications(result.data);
                const pending = result.data.filter((n) => n.status === 'Pending').length;
                setUnreadCount(pending);
            })
            .catch(() => {
                // silently fail
            });
    }, []);

    useEffect(() => {
        function handleClickOutside(e: MouseEvent) {
            if (ref.current && !ref.current.contains(e.target as Node)) {
                setOpen(false);
            }
        }
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    return (
        <div className="relative" ref={ref}>
            <button
                onClick={() => setOpen((o) => !o)}
                className="relative rounded-md p-2 text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                aria-label="Notifications"
            >
                <Bell size={18} />
                {unreadCount > 0 && (
                    <span className="absolute -right-0.5 -top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-destructive text-[10px] font-bold text-destructive-foreground">
                        {unreadCount > 9 ? '9+' : unreadCount}
                    </span>
                )}
            </button>

            {open && (
                <div className="absolute right-0 top-full z-50 mt-1 w-80 rounded-md border border-border bg-popover shadow-lg">
                    <div className="border-b border-border px-3 py-2">
                        <p className="text-sm font-medium text-popover-foreground">Notifications</p>
                    </div>
                    <div className="max-h-72 overflow-y-auto">
                        {notifications.length === 0 ? (
                            <p className="px-3 py-6 text-center text-sm text-muted-foreground">No notifications</p>
                        ) : (
                            notifications.map((n) => (
                                <div key={n.notificationLogId} className="flex items-start gap-2 border-b border-border px-3 py-2 last:border-0 hover:bg-accent/50">
                                    <div className="mt-0.5 text-muted-foreground">
                                        {channelIcons[n.channel] ?? <Bell size={12} />}
                                    </div>
                                    <div className="min-w-0 flex-1">
                                        <p className="text-xs font-medium text-popover-foreground truncate">{n.subject}</p>
                                        <p className="text-[10px] text-muted-foreground">{new Date(n.dateCreated).toLocaleString()}</p>
                                    </div>
                                    <div className="shrink-0">{statusIcons[n.status]}</div>
                                </div>
                            ))
                        )}
                    </div>
                    <div className="border-t border-border">
                        <button
                            onClick={() => { setOpen(false); navigate('/notifications'); }}
                            className="w-full px-3 py-2 text-center text-xs font-medium text-primary hover:bg-accent"
                        >
                            View All
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}
