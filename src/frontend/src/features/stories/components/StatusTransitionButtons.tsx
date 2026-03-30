import { useState } from 'react';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { Modal } from '@/components/common/Modal';
import { MemberSelector } from '@/components/forms/MemberSelector';
import { getValidTransitions } from '@/utils/workflowStateMachine';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { useAuth } from '@/hooks/useAuth';
import type { StoryStatus } from '@/types/enums';
import { ArrowRight, UserPlus } from 'lucide-react';

interface StatusTransitionButtonsProps {
    storyId: string;
    currentStatus: StoryStatus;
    assigneeId: string | null;
    onStatusChanged: () => void;
}

export function StatusTransitionButtons({
    storyId,
    currentStatus,
    assigneeId,
    onStatusChanged,
}: StatusTransitionButtonsProps) {
    const { addToast } = useToast();
    const { user } = useAuth();
    const [transitioning, setTransitioning] = useState<string | null>(null);
    const [assignOpen, setAssignOpen] = useState(false);
    const [assigning, setAssigning] = useState(false);

    const validTransitions = getValidTransitions('Story', currentStatus) as StoryStatus[];
    const canAssign = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead';

    const handleTransition = async (newStatus: StoryStatus) => {
        setTransitioning(newStatus);
        try {
            await workApi.updateStoryStatus(storyId, { status: newStatus });
            addToast('success', `Status changed to ${newStatus}`);
            onStatusChanged();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', 'Failed to update status');
            }
        } finally {
            setTransitioning(null);
        }
    };

    const handleAssign = async (memberId: string) => {
        setAssigning(true);
        try {
            await workApi.assignStory(storyId, { assigneeId: memberId });
            addToast('success', 'Story assigned');
            setAssignOpen(false);
            onStatusChanged();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', 'Failed to assign story');
            }
        } finally {
            setAssigning(false);
        }
    };

    if (validTransitions.length === 0 && !canAssign) return null;

    return (
        <div className="flex flex-wrap items-center gap-2">
            {validTransitions.map((status) => (
                <button
                    key={status}
                    onClick={() => handleTransition(status)}
                    disabled={transitioning !== null}
                    className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                >
                    <ArrowRight size={14} />
                    {transitioning === status ? 'Updating...' : `Move to ${status}`}
                </button>
            ))}

            {canAssign && (
                <button
                    onClick={() => setAssignOpen(true)}
                    className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-1.5 text-sm font-medium text-foreground hover:bg-accent"
                >
                    <UserPlus size={14} />
                    {assigneeId ? 'Reassign' : 'Assign'}
                </button>
            )}

            <Modal open={assignOpen} onClose={() => setAssignOpen(false)} title="Assign Story">
                <div className="space-y-4">
                    <MemberSelector
                        value={assigneeId ?? undefined}
                        onSelect={(memberId) => handleAssign(memberId)}
                        placeholder="Search for a member..."
                    />
                    {assigning && <p className="text-sm text-muted-foreground">Assigning...</p>}
                </div>
            </Modal>
        </div>
    );
}
