import { useState, useEffect, useCallback } from 'react';
import { profileApi } from '@/api/profileApi';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { NotificationSetting } from '@/types/profile';

const typeLabels: Record<string, string> = {
    StoryAssigned: 'Story Assigned',
    TaskAssigned: 'Task Assigned',
    SprintStarted: 'Sprint Started',
    SprintEnded: 'Sprint Ended',
    MentionedInComment: 'Mentioned in Comment',
    StoryStatusChanged: 'Story Status Changed',
    TaskStatusChanged: 'Task Status Changed',
    DueDateApproaching: 'Due Date Approaching',
};

export function NotificationPreferencesTable() {
    const { addToast } = useToast();
    const [settings, setSettings] = useState<NotificationSetting[]>([]);
    const [loading, setLoading] = useState(true);
    const [updating, setUpdating] = useState<string | null>(null);

    const fetchSettings = useCallback(async () => {
        setLoading(true);
        try {
            const data = await profileApi.getNotificationSettings();
            setSettings(data);
        } catch {
            addToast('error', 'Failed to load notification settings');
        } finally {
            setLoading(false);
        }
    }, [addToast]);

    useEffect(() => { fetchSettings(); }, [fetchSettings]);

    const handleToggle = async (
        setting: NotificationSetting,
        channel: 'emailEnabled' | 'pushEnabled' | 'inAppEnabled'
    ) => {
        const updated = { ...setting, [channel]: !setting[channel] };
        setUpdating(setting.notificationTypeId + channel);
        // Optimistic update
        setSettings((prev) =>
            prev.map((s) => (s.notificationTypeId === setting.notificationTypeId ? updated : s))
        );
        try {
            await profileApi.updateNotificationSetting(setting.notificationTypeId, {
                emailEnabled: updated.emailEnabled,
                pushEnabled: updated.pushEnabled,
                inAppEnabled: updated.inAppEnabled,
            });
        } catch (err) {
            // Revert on failure
            setSettings((prev) =>
                prev.map((s) => (s.notificationTypeId === setting.notificationTypeId ? setting : s))
            );
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to update notification setting');
        } finally {
            setUpdating(null);
        }
    };

    if (loading) return <SkeletonLoader variant="table" rows={8} columns={4} />;

    return (
        <div className="overflow-x-auto rounded-md border border-border">
            <table className="w-full text-sm">
                <thead>
                    <tr className="border-b border-border bg-muted/50">
                        <th className="px-4 py-3 text-left font-medium text-muted-foreground">Notification Type</th>
                        <th className="px-4 py-3 text-center font-medium text-muted-foreground">Email</th>
                        <th className="px-4 py-3 text-center font-medium text-muted-foreground">Push</th>
                        <th className="px-4 py-3 text-center font-medium text-muted-foreground">In-App</th>
                    </tr>
                </thead>
                <tbody>
                    {settings.map((s) => (
                        <tr key={s.notificationTypeId} className="border-b border-border last:border-0">
                            <td className="px-4 py-3 text-foreground">{typeLabels[s.typeName] ?? s.typeName}</td>
                            <td className="px-4 py-3 text-center">
                                <ToggleSwitch
                                    checked={s.emailEnabled}
                                    disabled={updating === s.notificationTypeId + 'emailEnabled'}
                                    onChange={() => handleToggle(s, 'emailEnabled')}
                                    label={`Toggle email for ${s.typeName}`}
                                />
                            </td>
                            <td className="px-4 py-3 text-center">
                                <ToggleSwitch
                                    checked={s.pushEnabled}
                                    disabled={updating === s.notificationTypeId + 'pushEnabled'}
                                    onChange={() => handleToggle(s, 'pushEnabled')}
                                    label={`Toggle push for ${s.typeName}`}
                                />
                            </td>
                            <td className="px-4 py-3 text-center">
                                <ToggleSwitch
                                    checked={s.inAppEnabled}
                                    disabled={updating === s.notificationTypeId + 'inAppEnabled'}
                                    onChange={() => handleToggle(s, 'inAppEnabled')}
                                    label={`Toggle in-app for ${s.typeName}`}
                                />
                            </td>
                        </tr>
                    ))}
                    {settings.length === 0 && (
                        <tr>
                            <td colSpan={4} className="px-4 py-8 text-center text-muted-foreground">No notification settings available</td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
}

function ToggleSwitch({ checked, disabled, onChange, label }: { checked: boolean; disabled?: boolean; onChange: () => void; label: string }) {
    return (
        <button
            type="button"
            role="switch"
            aria-checked={checked}
            aria-label={label}
            disabled={disabled}
            onClick={onChange}
            className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer items-center rounded-full transition-colors disabled:opacity-50 disabled:cursor-not-allowed ${checked ? 'bg-primary' : 'bg-muted'}`}
        >
            <span className={`inline-block h-3.5 w-3.5 rounded-full bg-white shadow transition-transform ${checked ? 'translate-x-4.5' : 'translate-x-0.5'}`} />
        </button>
    );
}
