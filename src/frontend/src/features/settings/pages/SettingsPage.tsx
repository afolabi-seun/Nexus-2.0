import { useState, useEffect, useCallback } from 'react';
import { profileApi } from '@/api/profileApi';
import { FormField } from '@/components/forms/FormField';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { useOrg } from '@/hooks/useOrg';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { OrganizationSettings, UpdateOrganizationSettingsRequest } from '@/types/profile';
import { WorkflowSection } from '../components/WorkflowSection';
import { SnapshotStatusSection } from '../components/SnapshotStatusSection';

export function SettingsPage() {
    const { addToast } = useToast();
    const { organization } = useOrg();
    const [settings, setSettings] = useState<OrganizationSettings | null>(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [form, setForm] = useState<UpdateOrganizationSettingsRequest>({});

    const fetchSettings = useCallback(async () => {
        if (!organization) return;
        setLoading(true);
        try {
            const data = await profileApi.getOrganizationSettings(organization.organizationId);
            setSettings(data);
            setForm({
                storyPointScale: data.storyPointScale ?? undefined,
                autoAssignmentEnabled: data.autoAssignmentEnabled,
                autoAssignmentStrategy: data.autoAssignmentStrategy ?? undefined,
                workingDays: data.workingDays ?? undefined,
                workingHoursStart: data.workingHoursStart ?? undefined,
                workingHoursEnd: data.workingHoursEnd ?? undefined,
                primaryColor: data.primaryColor ?? undefined,
                defaultBoardView: data.defaultBoardView ?? undefined,
                wipLimitsEnabled: data.wipLimitsEnabled,
                defaultWipLimit: data.defaultWipLimit,
                defaultNotificationChannels: data.defaultNotificationChannels ?? undefined,
                digestFrequency: data.digestFrequency ?? undefined,
                auditRetentionDays: data.auditRetentionDays,
            });
        } catch {
            addToast('error', 'Failed to load settings');
        } finally {
            setLoading(false);
        }
    }, [organization, addToast]);

    useEffect(() => { fetchSettings(); }, [fetchSettings]);

    const handleSave = async () => {
        if (!organization) return;
        setSaving(true);
        try {
            const updated = await profileApi.updateOrganizationSettings(organization.organizationId, form);
            setSettings(updated);
            addToast('success', 'Settings saved');
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to save settings');
        } finally {
            setSaving(false);
        }
    };

    const updateField = <K extends keyof UpdateOrganizationSettingsRequest>(key: K, value: UpdateOrganizationSettingsRequest[K]) => {
        setForm((f) => ({ ...f, [key]: value }));
    };

    if (loading) return <SkeletonLoader variant="form" />;
    if (!settings || !organization) return <div className="py-12 text-center text-muted-foreground">Settings not available</div>;

    return (
        <div className="space-y-6">
            <h1 className="text-2xl font-semibold text-foreground">Organization Settings</h1>
            <p className="text-sm text-muted-foreground">{organization.name}</p>

            {/* General */}
            <section className="space-y-4 rounded-md border border-border p-4">
                <h2 className="text-lg font-medium text-foreground">General</h2>
                <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-1">
                        <label className="text-sm font-medium text-foreground">Story ID Prefix</label>
                        <input value={organization.storyIdPrefix} disabled className="h-9 w-full rounded-md border border-input bg-muted px-3 text-sm text-muted-foreground" />
                        <p className="text-xs text-muted-foreground">Cannot be changed after stories are created</p>
                    </div>
                    <div className="space-y-1">
                        <label className="text-sm font-medium text-foreground">Timezone</label>
                        <input value={organization.timeZone} disabled className="h-9 w-full rounded-md border border-input bg-muted px-3 text-sm text-muted-foreground" />
                    </div>
                </div>
            </section>

            {/* Workflow */}
            <section className="space-y-4 rounded-md border border-border p-4">
                <h2 className="text-lg font-medium text-foreground">Workflow</h2>
                <div className="grid grid-cols-2 gap-4">
                    <FormField name="storyPointScale" label="Story Point Scale">
                        <input value={form.storyPointScale ?? ''} onChange={(e) => updateField('storyPointScale', e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" placeholder="1,2,3,5,8,13,21" />
                    </FormField>
                    <div className="space-y-1">
                        <label className="flex items-center gap-2 text-sm font-medium text-foreground">
                            <input type="checkbox" checked={form.autoAssignmentEnabled ?? false} onChange={(e) => updateField('autoAssignmentEnabled', e.target.checked)} className="rounded border-input" />
                            Auto-Assignment Enabled
                        </label>
                    </div>
                    <FormField name="autoAssignmentStrategy" label="Auto-Assignment Strategy">
                        <select value={form.autoAssignmentStrategy ?? ''} onChange={(e) => updateField('autoAssignmentStrategy', e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                            <option value="">None</option>
                            <option value="RoundRobin">Round Robin</option>
                            <option value="LeastBusy">Least Busy</option>
                        </select>
                    </FormField>
                    <FormField name="workingHoursStart" label="Working Hours Start">
                        <input type="time" value={form.workingHoursStart ?? ''} onChange={(e) => updateField('workingHoursStart', e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                    </FormField>
                    <FormField name="workingHoursEnd" label="Working Hours End">
                        <input type="time" value={form.workingHoursEnd ?? ''} onChange={(e) => updateField('workingHoursEnd', e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                    </FormField>
                </div>
            </section>

            {/* Workflow Transitions (read-only view from WorkService) */}
            <WorkflowSection />

            {/* Board */}
            <section className="space-y-4 rounded-md border border-border p-4">
                <h2 className="text-lg font-medium text-foreground">Board</h2>
                <div className="grid grid-cols-2 gap-4">
                    <FormField name="defaultBoardView" label="Default Board View">
                        <select value={form.defaultBoardView ?? ''} onChange={(e) => updateField('defaultBoardView', e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                            <option value="">None</option>
                            <option value="Kanban">Kanban</option>
                            <option value="Sprint">Sprint</option>
                            <option value="Backlog">Backlog</option>
                        </select>
                    </FormField>
                    <div className="space-y-1">
                        <label className="flex items-center gap-2 text-sm font-medium text-foreground">
                            <input type="checkbox" checked={form.wipLimitsEnabled ?? false} onChange={(e) => updateField('wipLimitsEnabled', e.target.checked)} className="rounded border-input" />
                            WIP Limits Enabled
                        </label>
                    </div>
                    <FormField name="defaultWipLimit" label="Default WIP Limit">
                        <input type="number" min={1} value={form.defaultWipLimit ?? 0} onChange={(e) => updateField('defaultWipLimit', Number(e.target.value))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                    </FormField>
                    <FormField name="primaryColor" label="Primary Color">
                        <input type="color" value={form.primaryColor ?? '#3b82f6'} onChange={(e) => updateField('primaryColor', e.target.value)} className="h-9 w-16 rounded-md border border-input bg-background" />
                    </FormField>
                </div>
            </section>

            {/* Notification */}
            <section className="space-y-4 rounded-md border border-border p-4">
                <h2 className="text-lg font-medium text-foreground">Notification</h2>
                <div className="grid grid-cols-2 gap-4">
                    <FormField name="defaultNotificationChannels" label="Default Notification Channels">
                        <input value={form.defaultNotificationChannels ?? ''} onChange={(e) => updateField('defaultNotificationChannels', e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" placeholder="Email,Push,InApp" />
                    </FormField>
                    <FormField name="digestFrequency" label="Digest Frequency">
                        <select value={form.digestFrequency ?? ''} onChange={(e) => updateField('digestFrequency', e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                            <option value="">None</option>
                            <option value="Realtime">Realtime</option>
                            <option value="Hourly">Hourly</option>
                            <option value="Daily">Daily</option>
                            <option value="Off">Off</option>
                        </select>
                    </FormField>
                </div>
            </section>

            {/* Data */}
            <section className="space-y-4 rounded-md border border-border p-4">
                <h2 className="text-lg font-medium text-foreground">Data</h2>
                <FormField name="auditRetentionDays" label="Audit Retention Days">
                    <input type="number" min={1} value={form.auditRetentionDays ?? 90} onChange={(e) => updateField('auditRetentionDays', Number(e.target.value))} className="h-9 w-full max-w-xs rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                </FormField>
            </section>

            {/* Analytics Snapshot Status */}
            <SnapshotStatusSection />

            <div className="flex justify-end">
                <button onClick={handleSave} disabled={saving} className="rounded-md bg-primary px-6 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                    {saving ? 'Saving...' : 'Save Settings'}
                </button>
            </div>
        </div>
    );
}
