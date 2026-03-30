import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { profileApi } from '@/api/profileApi';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { useAuth } from '@/hooks/useAuth';
import { useOrg } from '@/hooks/useOrg';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { TeamMemberDetail } from '@/types/profile';
import { User, Briefcase, Calendar } from 'lucide-react';

export function MemberProfilePage() {
    const { id } = useParams<{ id: string }>();
    const { addToast } = useToast();
    const { user } = useAuth();
    const { departments } = useOrg();

    const [member, setMember] = useState<TeamMemberDetail | null>(null);
    const [loading, setLoading] = useState(true);
    const [roleDialogOpen, setRoleDialogOpen] = useState(false);
    const [addDeptOpen, setAddDeptOpen] = useState(false);
    const [selectedDeptId, setSelectedDeptId] = useState('');
    const [selectedRoleId, setSelectedRoleId] = useState('');
    const [editAvailability, setEditAvailability] = useState('');
    const [editMaxTasks, setEditMaxTasks] = useState(5);
    const [saving, setSaving] = useState(false);

    const isOrgAdmin = user?.roleName === 'OrgAdmin';
    const isSelf = user?.userId === id;

    const fetchMember = useCallback(async () => {
        if (!id) return;
        setLoading(true);
        try {
            const data = await profileApi.getTeamMember(id);
            setMember(data);
            setEditAvailability(data.availability);
            setEditMaxTasks(data.maxConcurrentTasks);
        } catch {
            addToast('error', 'Failed to load member profile');
        } finally {
            setLoading(false);
        }
    }, [id, addToast]);

    useEffect(() => { fetchMember(); }, [fetchMember]);

    const handleSelfUpdate = async () => {
        if (!id) return;
        setSaving(true);
        try {
            await profileApi.updateTeamMember(id, {
                availability: editAvailability as TeamMemberDetail['availability'],
                maxConcurrentTasks: editMaxTasks,
            });
            addToast('success', 'Profile updated');
            fetchMember();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to update profile');
        } finally {
            setSaving(false);
        }
    };

    const handleAddDepartment = async () => {
        if (!id || !selectedDeptId || !selectedRoleId) return;
        setSaving(true);
        try {
            await profileApi.addToDepartment(id, { departmentId: selectedDeptId, roleId: selectedRoleId });
            addToast('success', 'Added to department');
            setAddDeptOpen(false);
            fetchMember();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to add to department');
        } finally {
            setSaving(false);
        }
    };

    if (loading) return <SkeletonLoader variant="form" />;
    if (!member) return <div className="py-12 text-center text-muted-foreground">Member not found</div>;

    const capacityPct = member.maxConcurrentTasks > 0
        ? Math.min(100, Math.round((member.activeTaskCount / member.maxConcurrentTasks) * 100))
        : 0;

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-start gap-4">
                <div className="flex h-16 w-16 items-center justify-center rounded-full bg-primary text-xl font-semibold text-primary-foreground">
                    {member.firstName.charAt(0)}{member.lastName.charAt(0)}
                </div>
                <div className="flex-1">
                    <h1 className="text-2xl font-semibold text-foreground">{member.firstName} {member.lastName}</h1>
                    <p className="text-sm text-muted-foreground">{member.email}</p>
                    <div className="mt-1 flex flex-wrap gap-2">
                        <Badge variant="status" value={member.availability} />
                        <Badge variant="status" value={member.flgStatus === 'A' ? 'Active' : member.flgStatus === 'S' ? 'Suspended' : 'Deactivated'} />
                    </div>
                </div>
            </div>

            {/* Meta */}
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
                <MetaCard icon={<User size={14} />} label="Professional ID" value={member.professionalId} />
                <MetaCard icon={<Briefcase size={14} />} label="Department" value={member.departmentName ?? '—'} />
                <MetaCard label="Role" value={member.roleName ?? '—'} />
                <MetaCard icon={<Calendar size={14} />} label="Joined" value={new Date(member.dateCreated).toLocaleDateString()} />
            </div>

            {/* Capacity Bar */}
            <section className="space-y-2">
                <h2 className="text-sm font-medium text-muted-foreground">
                    Capacity ({member.activeTaskCount}/{member.maxConcurrentTasks} tasks)
                </h2>
                <div className="h-3 w-full overflow-hidden rounded-full bg-muted">
                    <div
                        className={`h-full rounded-full transition-all ${capacityPct >= 90 ? 'bg-destructive' : capacityPct >= 70 ? 'bg-yellow-500' : 'bg-primary'}`}
                        style={{ width: `${capacityPct}%` }}
                    />
                </div>
            </section>

            {/* Department Memberships */}
            <section className="space-y-2">
                <div className="flex items-center justify-between">
                    <h2 className="text-lg font-medium text-foreground">Department Memberships</h2>
                    {isOrgAdmin && (
                        <button onClick={() => setAddDeptOpen(true)} className="rounded-md border border-input px-3 py-1.5 text-sm font-medium text-foreground hover:bg-accent">
                            Add to Department
                        </button>
                    )}
                </div>
                {member.departmentMemberships.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No department memberships</p>
                ) : (
                    <div className="space-y-1.5">
                        {member.departmentMemberships.map((dm) => (
                            <div key={dm.departmentId} className="flex items-center justify-between rounded-md border border-border px-3 py-2">
                                <div>
                                    <span className="text-sm font-medium text-foreground">{dm.departmentName}</span>
                                    <Badge variant="role" value={dm.roleName} className="ml-2" />
                                </div>
                                {isOrgAdmin && (
                                    <button
                                        onClick={() => { setSelectedDeptId(dm.departmentId); setRoleDialogOpen(true); }}
                                        className="text-xs text-primary hover:underline"
                                    >
                                        Change Role
                                    </button>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </section>

            {/* Skills */}
            {member.skills && member.skills.length > 0 && (
                <section className="space-y-2">
                    <h2 className="text-lg font-medium text-foreground">Skills</h2>
                    <div className="flex flex-wrap gap-2">
                        {member.skills.map((skill) => (
                            <span key={skill} className="rounded-full bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">{skill}</span>
                        ))}
                    </div>
                </section>
            )}

            {/* Self-edit section */}
            {isSelf && (
                <section className="space-y-3 rounded-md border border-border p-4">
                    <h2 className="text-lg font-medium text-foreground">Edit Profile</h2>
                    <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-1">
                            <label className="text-sm font-medium text-foreground">Availability</label>
                            <select value={editAvailability} onChange={(e) => setEditAvailability(e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                                {['Available', 'Busy', 'Away', 'Offline'].map((a) => <option key={a} value={a}>{a}</option>)}
                            </select>
                        </div>
                        <div className="space-y-1">
                            <label className="text-sm font-medium text-foreground">Max Concurrent Tasks</label>
                            <input type="number" min={1} max={20} value={editMaxTasks} onChange={(e) => setEditMaxTasks(Number(e.target.value))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground" />
                        </div>
                    </div>
                    <button onClick={handleSelfUpdate} disabled={saving} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                        {saving ? 'Saving...' : 'Save Changes'}
                    </button>
                </section>
            )}

            {/* Change Role Modal */}
            <Modal open={roleDialogOpen} onClose={() => setRoleDialogOpen(false)} title="Change Role">
                <div className="space-y-4">
                    <select value={selectedRoleId} onChange={(e) => setSelectedRoleId(e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                        <option value="">Select Role</option>
                        <option value="OrgAdmin">OrgAdmin</option>
                        <option value="DeptLead">DeptLead</option>
                        <option value="Member">Member</option>
                        <option value="Viewer">Viewer</option>
                    </select>
                    <div className="flex justify-end gap-2">
                        <button onClick={() => setRoleDialogOpen(false)} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">Cancel</button>
                        <button
                            onClick={async () => {
                                if (!id || !selectedDeptId || !selectedRoleId) return;
                                try {
                                    await profileApi.changeRole(id, selectedDeptId, { roleId: selectedRoleId });
                                    addToast('success', 'Role changed');
                                    setRoleDialogOpen(false);
                                    fetchMember();
                                } catch (err) {
                                    if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
                                    else addToast('error', 'Failed to change role');
                                }
                            }}
                            disabled={!selectedRoleId}
                            className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                        >
                            Change Role
                        </button>
                    </div>
                </div>
            </Modal>

            {/* Add to Department Modal */}
            <Modal open={addDeptOpen} onClose={() => setAddDeptOpen(false)} title="Add to Department">
                <div className="space-y-4">
                    <select value={selectedDeptId} onChange={(e) => setSelectedDeptId(e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                        <option value="">Select Department</option>
                        {departments.map((d) => <option key={d.departmentId} value={d.departmentId}>{d.name}</option>)}
                    </select>
                    <select value={selectedRoleId} onChange={(e) => setSelectedRoleId(e.target.value)} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground">
                        <option value="">Select Role</option>
                        <option value="OrgAdmin">OrgAdmin</option>
                        <option value="DeptLead">DeptLead</option>
                        <option value="Member">Member</option>
                        <option value="Viewer">Viewer</option>
                    </select>
                    <div className="flex justify-end gap-2">
                        <button onClick={() => setAddDeptOpen(false)} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">Cancel</button>
                        <button onClick={handleAddDepartment} disabled={!selectedDeptId || !selectedRoleId || saving} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                            {saving ? 'Adding...' : 'Add'}
                        </button>
                    </div>
                </div>
            </Modal>
        </div>
    );
}

function MetaCard({ icon, label, value }: { icon?: React.ReactNode; label: string; value: string }) {
    return (
        <div className="rounded-lg border border-border bg-card p-3">
            <div className="flex items-center gap-1.5 text-xs text-muted-foreground">{icon}{label}</div>
            <p className="mt-0.5 text-sm font-medium text-card-foreground truncate">{value}</p>
        </div>
    );
}
