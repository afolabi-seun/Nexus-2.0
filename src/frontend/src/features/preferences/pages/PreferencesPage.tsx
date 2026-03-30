import { useState, useEffect, useCallback } from 'react';
import { profileApi } from '@/api/profileApi';
import { FormField } from '@/components/forms/FormField';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { useThemeStore } from '@/stores/themeStore';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { Theme, BoardView, DigestFrequency, DateFormat, TimeFormat } from '@/types/enums';
import type { UserPreferences, UpdatePreferencesRequest, ResolvedPreferences } from '@/types/profile';
import { NotificationPreferencesTable } from '../components/NotificationPreferencesTable.js';

export function PreferencesPage() {
    const { addToast } = useToast();
    const setTheme = useThemeStore((s) => s.setTheme);

    const [prefs, setPrefs] = useState<UserPreferences | null>(null);
    const [resolved, setResolved] = useState<ResolvedPreferences | null>(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [form, setForm] = useState<UpdatePreferencesRequest>({});

    const fetchPrefs = useCallback(async () => {
        setLoading(true);
        try {
            const [prefsData, resolvedData] = await Promise.all([
                profileApi.getPreferences(),
                profileApi.getResolvedPreferences().catch(() => null),
            ]);
            setPrefs(prefsData);
            setResolved(resolvedData);
            setForm({
                theme: prefsData.theme,
                language: prefsData.language,
                timezoneOverride: prefsData.timezoneOverride ?? undefined,
                defaultBoardView: prefsData.defaultBoardView ?? undefined,
                emailDigestFrequency: prefsData.emailDigestFrequency ?? undefined,
                keyboardShortcutsEnabled: prefsData.keyboardShortcutsEnabled,
                dateFormat: prefsData.dateFormat,
                timeFormat: prefsData.timeFormat,
            });
        } catch {
            addToast('error', 'Failed to load preferences');
        } finally {
            setLoading(false);
        }
    }, [addToast]);

    useEffect(() => { fetchPrefs(); }, [fetchPrefs]);

    const handleSave = async () => {
        setSaving(true);
        try {
            const updated = await profileApi.updatePreferences(form);
            setPrefs(updated);
            // Apply theme immediately
            if (form.theme) {
                setTheme(form.theme as 'Light' | 'Dark' | 'System');
            }
            addToast('success', 'Preferences saved');
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to save preferences');
        } finally {
            setSaving(false);
        }
    };

    const updateField = <K extends keyof UpdatePreferencesRequest>(key: K, value: UpdatePreferencesRequest[K]) => {
        setForm((f) => ({ ...f, [key]: value }));
        // Immediately apply theme change
        if (key === 'theme' && value) {
            setTheme(value as 'Light' | 'Dark' | 'System');
        }
    };

    if (loading) return <SkeletonLoader variant="form" />;
    if (!prefs) return <div className="py-12 text-center text-muted-foreground">Preferences not available</div>;

    return (
        <div className="space-y-6">
            <h1 className="text-2xl font-semibold text-foreground">Preferences</h1>

            <div className="space-y-4 rounded-md border border-border p-4">
                <div className="grid grid-cols-2 gap-4">
                    <FormField name="theme" label="Theme">
                        <select value={form.theme ?? ''} onChange={(e) => updateField('theme', e.target.value as Theme)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                            {Object.values(Theme).map((t) => <option key={t} value={t}>{t}</option>)}
                        </select>
                    </FormField>
                    <FormField name="language" label="Language">
                        <input value={form.language ?? ''} onChange={(e) => updateField('language', e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                    </FormField>
                    <FormField name="timezoneOverride" label="Timezone Override">
                        <input value={form.timezoneOverride ?? ''} onChange={(e) => updateField('timezoneOverride', e.target.value)} placeholder="e.g. America/New_York" className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                    </FormField>
                    <FormField name="defaultBoardView" label="Default Board View">
                        <select value={form.defaultBoardView ?? ''} onChange={(e) => updateField('defaultBoardView', (e.target.value || undefined) as BoardView | undefined)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                            <option value="">None</option>
                            {Object.values(BoardView).map((v) => <option key={v} value={v}>{v}</option>)}
                        </select>
                    </FormField>
                    <FormField name="emailDigestFrequency" label="Email Digest Frequency">
                        <select value={form.emailDigestFrequency ?? ''} onChange={(e) => updateField('emailDigestFrequency', (e.target.value || undefined) as DigestFrequency | undefined)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                            <option value="">None</option>
                            {Object.values(DigestFrequency).map((f) => <option key={f} value={f}>{f}</option>)}
                        </select>
                    </FormField>
                    <FormField name="dateFormat" label="Date Format">
                        <select value={form.dateFormat ?? ''} onChange={(e) => updateField('dateFormat', e.target.value as DateFormat)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                            {Object.values(DateFormat).map((f) => <option key={f} value={f}>{f}</option>)}
                        </select>
                    </FormField>
                    <FormField name="timeFormat" label="Time Format">
                        <select value={form.timeFormat ?? ''} onChange={(e) => updateField('timeFormat', e.target.value as TimeFormat)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                            {Object.values(TimeFormat).map((f) => <option key={f} value={f}>{f}</option>)}
                        </select>
                    </FormField>
                    <div className="flex items-center gap-2">
                        <input type="checkbox" id="kbShortcuts" checked={form.keyboardShortcutsEnabled ?? true} onChange={(e) => updateField('keyboardShortcutsEnabled', e.target.checked)} className="rounded border-input" />
                        <label htmlFor="kbShortcuts" className="text-sm font-medium text-foreground">Keyboard Shortcuts Enabled</label>
                    </div>
                </div>
            </div>

            <div className="flex justify-end">
                <button onClick={handleSave} disabled={saving} className="rounded-md bg-primary px-6 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                    {saving ? 'Saving...' : 'Save Preferences'}
                </button>
            </div>

            {/* Notification Preferences */}
            <section className="space-y-3">
                <h2 className="text-lg font-medium text-foreground">Notification Preferences</h2>
                <NotificationPreferencesTable />
            </section>

            {/* Resolved Preferences */}
            {resolved && (
                <section className="space-y-3 rounded-md border border-border p-4">
                    <h2 className="text-lg font-medium text-foreground">Effective Preferences</h2>
                    <p className="text-xs text-muted-foreground">These are the resolved preferences after applying organization and department defaults.</p>
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                        {Object.entries(resolved).map(([key, value]) => (
                            <div key={key} className="rounded border border-border px-3 py-2">
                                <p className="text-xs text-muted-foreground">{key}</p>
                                <p className="text-sm font-medium text-foreground">{String(value)}</p>
                            </div>
                        ))}
                    </div>
                </section>
            )}
        </div>
    );
}
