import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { profileApi } from '@/api/profileApi';
import { useOrg } from '@/hooks/useOrg';
import { useToast } from '@/components/common/Toast';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { CheckCircle2, ArrowRight, ArrowLeft, Rocket, FolderKanban, Users, Timer } from 'lucide-react';

const STEPS = ['Welcome', 'Create Project', 'Invite Members', 'Create Sprint', 'Done'];

function StepIndicator({ current, total }: { current: number; total: number }) {
    return (
        <div className="flex items-center gap-2">
            {Array.from({ length: total }, (_, i) => (
                <div key={i} className={`h-2 flex-1 rounded-full transition-colors ${i <= current ? 'bg-primary' : 'bg-muted'}`} />
            ))}
        </div>
    );
}

export function OnboardingWizard() {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const { organization, refresh } = useOrg();
    const [step, setStep] = useState(0);
    const [saving, setSaving] = useState(false);

    // Project form
    const [projectName, setProjectName] = useState('');
    const [projectKey, setProjectKey] = useState('');
    const [projectId, setProjectId] = useState('');

    // Invite form
    const [inviteEmail, setInviteEmail] = useState('');
    const [inviteFirst, setInviteFirst] = useState('');
    const [inviteLast, setInviteLast] = useState('');
    const [invitedCount, setInvitedCount] = useState(0);

    // Sprint form
    const [sprintName, setSprintName] = useState('Sprint 1');
    const [sprintStart, setSprintStart] = useState(new Date().toISOString().slice(0, 10));
    const [sprintEnd, setSprintEnd] = useState(new Date(Date.now() + 14 * 86400000).toISOString().slice(0, 10));

    const handleCreateProject = async () => {
        if (!projectName.trim() || !projectKey.trim()) return;
        setSaving(true);
        try {
            const project = await workApi.createProject({ name: projectName.trim(), projectKey: projectKey.trim().toUpperCase() });
            setProjectId(project.projectId);
            addToast('success', 'Project created!');
            setStep(2);
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to create project');
        } finally {
            setSaving(false);
        }
    };

    const handleInvite = async () => {
        if (!inviteEmail.trim() || !inviteFirst.trim() || !inviteLast.trim()) return;
        setSaving(true);
        try {
            await profileApi.createInvite({
                email: inviteEmail.trim(),
                firstName: inviteFirst.trim(),
                lastName: inviteLast.trim(),
                departmentId: '',
                roleId: 'Member',
            });
            addToast('success', `Invite sent to ${inviteEmail}`);
            setInvitedCount((c) => c + 1);
            setInviteEmail('');
            setInviteFirst('');
            setInviteLast('');
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to send invite');
        } finally {
            setSaving(false);
        }
    };

    const handleCreateSprint = async () => {
        if (!projectId || !sprintName.trim()) return;
        setSaving(true);
        try {
            await workApi.createSprint(projectId, {
                name: sprintName.trim(),
                startDate: sprintStart,
                endDate: sprintEnd,
            });
            addToast('success', 'Sprint created!');
            setStep(4);
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to create sprint');
        } finally {
            setSaving(false);
        }
    };

    const handleFinish = async () => {
        try { localStorage.setItem('nexus_onboarding_complete', '1'); } catch { /* ignore */ }
        await refresh();
        navigate('/');
    };

    return (
        <div className="mx-auto max-w-lg space-y-6 py-12">
            <StepIndicator current={step} total={STEPS.length} />

            {/* Step 0: Welcome */}
            {step === 0 && (
                <div className="space-y-4 text-center">
                    <Rocket size={48} className="mx-auto text-primary" />
                    <h1 className="text-2xl font-semibold text-foreground">Welcome to Nexus!</h1>
                    <p className="text-sm text-muted-foreground">
                        {organization?.name ? `${organization.name} is ready to go.` : 'Your organization is ready.'} Let's set up your workspace in a few quick steps.
                    </p>
                    <button onClick={() => setStep(1)} className="inline-flex items-center gap-2 rounded-md bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90">
                        Get Started <ArrowRight size={16} />
                    </button>
                </div>
            )}

            {/* Step 1: Create Project */}
            {step === 1 && (
                <div className="space-y-4">
                    <div className="flex items-center gap-3">
                        <FolderKanban size={24} className="text-primary" />
                        <h2 className="text-xl font-semibold text-foreground">Create Your First Project</h2>
                    </div>
                    <p className="text-sm text-muted-foreground">Projects contain stories, sprints, and boards. Give it a name and a short key (used in story IDs like PROJ-001).</p>
                    <div className="space-y-3">
                        <input value={projectName} onChange={(e) => setProjectName(e.target.value)} placeholder="Project name" className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                        <input value={projectKey} onChange={(e) => setProjectKey(e.target.value.toUpperCase())} placeholder="Project key (e.g. PROJ)" maxLength={10} className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground uppercase focus:outline-none focus:ring-2 focus:ring-ring" />
                    </div>
                    <div className="flex justify-between">
                        <button onClick={() => setStep(0)} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"><ArrowLeft size={14} /> Back</button>
                        <button onClick={handleCreateProject} disabled={saving || !projectName.trim() || !projectKey.trim()} className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                            {saving ? 'Creating...' : 'Create Project'} <ArrowRight size={14} />
                        </button>
                    </div>
                </div>
            )}

            {/* Step 2: Invite Members */}
            {step === 2 && (
                <div className="space-y-4">
                    <div className="flex items-center gap-3">
                        <Users size={24} className="text-primary" />
                        <h2 className="text-xl font-semibold text-foreground">Invite Your Team</h2>
                    </div>
                    <p className="text-sm text-muted-foreground">Invite team members by email. They'll receive an invitation to join your organization. {invitedCount > 0 && `(${invitedCount} invited so far)`}</p>
                    <div className="space-y-3">
                        <input value={inviteEmail} onChange={(e) => setInviteEmail(e.target.value)} placeholder="Email address" type="email" className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                        <div className="grid grid-cols-2 gap-3">
                            <input value={inviteFirst} onChange={(e) => setInviteFirst(e.target.value)} placeholder="First name" className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                            <input value={inviteLast} onChange={(e) => setInviteLast(e.target.value)} placeholder="Last name" className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                        </div>
                        <button onClick={handleInvite} disabled={saving || !inviteEmail.trim() || !inviteFirst.trim() || !inviteLast.trim()} className="w-full rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent disabled:opacity-50">
                            {saving ? 'Sending...' : 'Send Invite'}
                        </button>
                    </div>
                    <div className="flex justify-between">
                        <button onClick={() => setStep(1)} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"><ArrowLeft size={14} /> Back</button>
                        <button onClick={() => setStep(3)} className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90">
                            {invitedCount > 0 ? 'Continue' : 'Skip for now'} <ArrowRight size={14} />
                        </button>
                    </div>
                </div>
            )}

            {/* Step 3: Create Sprint */}
            {step === 3 && (
                <div className="space-y-4">
                    <div className="flex items-center gap-3">
                        <Timer size={24} className="text-primary" />
                        <h2 className="text-xl font-semibold text-foreground">Create Your First Sprint</h2>
                    </div>
                    <p className="text-sm text-muted-foreground">Sprints are time-boxed iterations (usually 1–2 weeks). You'll add stories to the sprint later.</p>
                    <div className="space-y-3">
                        <input value={sprintName} onChange={(e) => setSprintName(e.target.value)} placeholder="Sprint name" className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                        <div className="grid grid-cols-2 gap-3">
                            <div className="space-y-1">
                                <label className="text-xs text-muted-foreground">Start date</label>
                                <input type="date" value={sprintStart} onChange={(e) => setSprintStart(e.target.value)} className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                            </div>
                            <div className="space-y-1">
                                <label className="text-xs text-muted-foreground">End date</label>
                                <input type="date" value={sprintEnd} onChange={(e) => setSprintEnd(e.target.value)} className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                            </div>
                        </div>
                    </div>
                    <div className="flex justify-between">
                        <button onClick={() => setStep(2)} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"><ArrowLeft size={14} /> Back</button>
                        <div className="flex gap-2">
                            <button onClick={() => setStep(4)} className="text-sm text-muted-foreground hover:text-foreground">Skip</button>
                            <button onClick={handleCreateSprint} disabled={saving || !sprintName.trim()} className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                                {saving ? 'Creating...' : 'Create Sprint'} <ArrowRight size={14} />
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Step 4: Done */}
            {step === 4 && (
                <div className="space-y-4 text-center">
                    <CheckCircle2 size={48} className="mx-auto text-green-600" />
                    <h2 className="text-xl font-semibold text-foreground">You're All Set!</h2>
                    <p className="text-sm text-muted-foreground">
                        Your workspace is ready. Here's what you can do next:
                    </p>
                    <div className="space-y-2 text-left">
                        <SuggestionLink label="Create stories" description="Add work items to your project" onClick={() => { handleFinish(); navigate('/stories'); }} />
                        <SuggestionLink label="Set up your board" description="View and organize stories on the Kanban board" onClick={() => { handleFinish(); navigate('/boards/kanban'); }} />
                        <SuggestionLink label="Configure settings" description="Set sprint duration, story point scale, and more" onClick={() => { handleFinish(); navigate('/settings'); }} />
                    </div>
                    <button onClick={handleFinish} className="inline-flex items-center gap-2 rounded-md bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90">
                        Go to Dashboard <ArrowRight size={16} />
                    </button>
                </div>
            )}
        </div>
    );
}

function SuggestionLink({ label, description, onClick }: { label: string; description: string; onClick: () => void }) {
    return (
        <button onClick={onClick} className="flex w-full items-center justify-between rounded-md border border-border p-3 text-left hover:bg-accent">
            <div>
                <p className="text-sm font-medium text-foreground">{label}</p>
                <p className="text-xs text-muted-foreground">{description}</p>
            </div>
            <ArrowRight size={14} className="text-muted-foreground" />
        </button>
    );
}
