import { useState, useEffect, useCallback } from 'react';
import { profileApi } from '@/api/profileApi';
import { securityApi } from '@/api/securityApi';
import { Badge } from '@/components/common/Badge';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { Device } from '@/types/profile';
import type { SessionResponse } from '@/types/auth';
import { Monitor, Smartphone, Tablet, Star, Trash2, Shield, X } from 'lucide-react';

function DeviceIcon({ type }: { type: string }) {
    const lower = type.toLowerCase();
    if (lower.includes('mobile') || lower.includes('phone')) return <Smartphone size={18} />;
    if (lower.includes('tablet')) return <Tablet size={18} />;
    return <Monitor size={18} />;
}

export function SessionManagementPage() {
    const { addToast } = useToast();
    const [devices, setDevices] = useState<Device[]>([]);
    const [sessions, setSessions] = useState<SessionResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [confirmAction, setConfirmAction] = useState<{ type: string; id: string; message: string } | null>(null);

    const fetchData = useCallback(async () => {
        setLoading(true);
        try {
            const [devData, sessData] = await Promise.all([
                profileApi.getDevices().catch(() => []),
                securityApi.getSessions().catch(() => []),
            ]);
            setDevices(devData);
            setSessions(sessData);
        } catch {
            addToast('error', 'Failed to load session data');
        } finally {
            setLoading(false);
        }
    }, [addToast]);

    useEffect(() => { fetchData(); }, [fetchData]);

    const handleConfirm = async () => {
        if (!confirmAction) return;
        try {
            switch (confirmAction.type) {
                case 'removeDevice':
                    await profileApi.removeDevice(confirmAction.id);
                    addToast('success', 'Device removed');
                    break;
                case 'revokeSession':
                    await securityApi.revokeSession(confirmAction.id);
                    addToast('success', 'Session revoked');
                    break;
                case 'revokeAll':
                    await securityApi.revokeAllSessions();
                    addToast('success', 'All other sessions revoked');
                    break;
            }
            setConfirmAction(null);
            fetchData();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Operation failed');
        }
    };

    const handleSetPrimary = async (deviceId: string) => {
        try {
            await profileApi.setPrimaryDevice(deviceId);
            addToast('success', 'Primary device updated');
            fetchData();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to set primary device');
        }
    };

    if (loading) return <SkeletonLoader variant="form" />;

    return (
        <div className="space-y-6">
            <h1 className="text-2xl font-semibold text-foreground">Sessions & Devices</h1>

            {/* Devices */}
            <section className="space-y-3">
                <h2 className="text-lg font-medium text-foreground">Devices</h2>
                {devices.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No devices registered</p>
                ) : (
                    <div className="space-y-2">
                        {devices.map((d) => (
                            <div key={d.deviceId} className="flex items-center justify-between rounded-md border border-border p-3">
                                <div className="flex items-center gap-3">
                                    <div className="text-muted-foreground"><DeviceIcon type={d.deviceType} /></div>
                                    <div>
                                        <div className="flex items-center gap-2">
                                            <span className="text-sm font-medium text-foreground">{d.deviceName}</span>
                                            {d.isPrimary && <Badge value="Primary" />}
                                        </div>
                                        <p className="text-xs text-muted-foreground">
                                            {d.deviceType} · {d.ipAddress ?? 'Unknown IP'} · Last active: {new Date(d.lastActiveDate).toLocaleString()}
                                        </p>
                                    </div>
                                </div>
                                <div className="flex gap-2">
                                    {!d.isPrimary && (
                                        <button onClick={() => handleSetPrimary(d.deviceId)} className="inline-flex items-center gap-1 rounded-md border border-input px-2 py-1 text-xs font-medium text-foreground hover:bg-accent">
                                            <Star size={12} /> Set Primary
                                        </button>
                                    )}
                                    <button onClick={() => setConfirmAction({ type: 'removeDevice', id: d.deviceId, message: `Remove device "${d.deviceName}"?` })} className="inline-flex items-center gap-1 rounded-md border border-input px-2 py-1 text-xs font-medium text-destructive hover:bg-accent">
                                        <Trash2 size={12} /> Remove
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </section>

            {/* Sessions */}
            <section className="space-y-3">
                <div className="flex items-center justify-between">
                    <h2 className="text-lg font-medium text-foreground">Active Sessions</h2>
                    {sessions.length > 1 && (
                        <button onClick={() => setConfirmAction({ type: 'revokeAll', id: '', message: 'Revoke all other sessions?' })} className="inline-flex items-center gap-1.5 rounded-md border border-destructive px-3 py-1.5 text-sm font-medium text-destructive hover:bg-destructive/10">
                            <Shield size={14} /> Revoke All Others
                        </button>
                    )}
                </div>
                {sessions.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No active sessions</p>
                ) : (
                    <div className="space-y-2">
                        {sessions.map((s) => (
                            <div key={s.sessionId} className="flex items-center justify-between rounded-md border border-border p-3">
                                <div>
                                    <p className="text-sm font-medium text-foreground">{s.deviceInfo ?? 'Unknown Device'}</p>
                                    <p className="text-xs text-muted-foreground">
                                        IP: {s.ipAddress ?? 'Unknown'} · Created: {new Date(s.createdAt).toLocaleString()}
                                    </p>
                                </div>
                                <button onClick={() => setConfirmAction({ type: 'revokeSession', id: s.sessionId, message: 'Revoke this session?' })} className="inline-flex items-center gap-1 rounded-md border border-input px-2 py-1 text-xs font-medium text-destructive hover:bg-accent">
                                    <X size={12} /> Revoke
                                </button>
                            </div>
                        ))}
                    </div>
                )}
            </section>

            <ConfirmDialog
                open={confirmAction !== null}
                onConfirm={handleConfirm}
                onCancel={() => setConfirmAction(null)}
                title="Confirm Action"
                message={confirmAction?.message ?? ''}
                confirmLabel="Confirm"
            />
        </div>
    );
}
