import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { profileApi } from '@/api/profileApi';
import { Badge } from '@/components/common/Badge';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { useAuth } from '@/hooks/useAuth';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { Department, DepartmentPreferences, TeamMember } from '@/types/profile';
import { DepartmentWorkloadSection } from '../components/DepartmentWorkloadSection';

export function DepartmentDetailPage() {
    const { id } = useParams<{ id: string }>();
    const { addToast } = useToast();
    const { user } = useAuth();

    const [dept, setDept] = useState<Department | null>(null);
    const [prefs, setPrefs] = useState<DepartmentPreferences | null>(null);
    const [members, setMembers] = useState<TeamMember[]>([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [editPrefs, setEditPrefs] = useState<Partial<DepartmentPreferences>>({});

    const canEditPrefs = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead';

    const fetchData = useCallback(async () => {
        if (!id) return;
        setLoading(true);
        try {
            const [deptData, prefsData, membersData] = await Promise.all([
                profileApi.getDepartment(id),
                profileApi.getDepartmentPreferences(id).catch(() => null),
                profileApi.getTeamMembers({ departmentId: id, page: 1, pageSize: 50 }).then((r) => r.data).catch(() => []),
            ]);
            setDept(deptData);
            setPrefs(prefsData);
            setEditPrefs(prefsData ?? {});
            setMembers(membersData);
        } catch {
            addToast('error', 'Failed to load department');
        } finally {
            setLoading(false);
        }
    }, [id, addToast]);

    useEffect(() => { fetchData(); }, [fetchData]);

    const handleSavePrefs = async () => {
        if (!id) return;
        setSaving(true);
        try {
            const updated = await profileApi.updateDepartmentPreferences(id, {
                maxConcurrentTasksDefault: editPrefs.maxConcurrentTasksDefault,
            });
            setPrefs(updated);
            addToast('success', 'Preferences updated');
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to update preferences');
        } finally {
            setSaving(false);
        }
    };

    if (loading) return <SkeletonLoader variant="form" />;
    if (!dept) return <div className="py-12 text-center text-muted-foreground">Department not found</div>;

    return (
        <div className="space-y-6">
            <div className="flex items-start justify-between">
                <div>
                    <h1 className="text-2xl font-semibold text-foreground">{dept.name}</h1>
                    <p className="text-sm text-muted-foreground">Code: {dept.code}</p>
                </div>
                <div className="flex gap-2">
                    <Badge variant="status" value={dept.flgStatus === 'A' ? 'Active' : 'Inactive'} />
                    {dept.isDefault && <Badge value="Default" />}
                </div>
            </div>

            {dept.description && (
                <p className="text-sm text-muted-foreground">{dept.description}</p>
            )}

            <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
                <div className="rounded-lg border border-border bg-card p-3">
                    <p className="text-xs text-muted-foreground">Members</p>
                    <p className="text-lg font-semibold text-card-foreground">{dept.memberCount}</p>
                </div>
            </div>

            {/* Workload */}
            <DepartmentWorkloadSection departmentId={id!} />

            {/* Members */}
            <section className="space-y-2">
                <h2 className="text-lg font-medium text-foreground">Members</h2>
                {members.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No members</p>
                ) : (
                    <div className="space-y-1.5">
                        {members.map((m) => (
                            <div key={m.teamMemberId} className="flex items-center justify-between rounded-md border border-border px-3 py-2">
                                <div className="flex items-center gap-2">
                                    <span className="flex h-7 w-7 items-center justify-center rounded-full bg-primary text-xs font-medium text-primary-foreground">
                                        {m.firstName.charAt(0)}{m.lastName.charAt(0)}
                                    </span>
                                    <span className="text-sm text-foreground">{m.firstName} {m.lastName}</span>
                                </div>
                                <div className="flex items-center gap-2">
                                    {m.roleName && <Badge variant="role" value={m.roleName} />}
                                    <Badge variant="status" value={m.availability} />
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </section>

            {/* Preferences */}
            {canEditPrefs && (
                <section className="space-y-3 rounded-md border border-border p-4">
                    <h2 className="text-lg font-medium text-foreground">Department Preferences</h2>
                    {dept.isDefault && (
                        <p className="text-xs text-muted-foreground">Default departments cannot be deleted.</p>
                    )}
                    <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-1">
                            <label className="text-sm font-medium text-foreground">Max Concurrent Tasks Default</label>
                            <input
                                type="number"
                                min={1}
                                max={20}
                                value={editPrefs.maxConcurrentTasksDefault ?? prefs?.maxConcurrentTasksDefault ?? 5}
                                onChange={(e) => setEditPrefs((p) => ({ ...p, maxConcurrentTasksDefault: Number(e.target.value) }))}
                                className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground"
                            />
                        </div>
                    </div>
                    <button onClick={handleSavePrefs} disabled={saving} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                        {saving ? 'Saving...' : 'Save Preferences'}
                    </button>
                </section>
            )}
        </div>
    );
}
