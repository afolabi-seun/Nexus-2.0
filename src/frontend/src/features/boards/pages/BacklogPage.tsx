import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { SprintStatus } from '@/types/enums';
import type { Backlog, BacklogItem, SprintListItem, BoardFilters as BoardFiltersType } from '@/types/work';
import { BoardFilters } from '../components/BoardFilters.js';
import { SaveFilterDialog } from '@/features/filters/components/SaveFilterDialog';
import { SavedFilterDropdown } from '@/features/filters/components/SavedFilterDropdown';
import { Plus } from 'lucide-react';

export function BacklogPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const [backlog, setBacklog] = useState<Backlog | null>(null);
    const [loading, setLoading] = useState(true);
    const [filters, setFilters] = useState<BoardFiltersType>({});
    const [addToSprintOpen, setAddToSprintOpen] = useState(false);
    const [selectedStory, setSelectedStory] = useState<BacklogItem | null>(null);
    const [sprints, setSprints] = useState<SprintListItem[]>([]);
    const [selectedSprintId, setSelectedSprintId] = useState('');
    const [adding, setAdding] = useState(false);

    const fetchBacklog = useCallback(async () => {
        setLoading(true);
        try {
            const data = await workApi.getBacklog(filters);
            setBacklog(data);
        } catch {
            addToast('error', 'Failed to load backlog');
        } finally {
            setLoading(false);
        }
    }, [filters, addToast]);

    useEffect(() => { fetchBacklog(); }, [fetchBacklog]);

    const openAddToSprint = async (story: BacklogItem) => {
        setSelectedStory(story);
        try {
            const res = await workApi.getSprints({ status: SprintStatus.Planning, page: 1, pageSize: 50 });
            setSprints(res.data);
        } catch {
            setSprints([]);
        }
        setAddToSprintOpen(true);
    };

    const handleAddToSprint = async () => {
        if (!selectedStory || !selectedSprintId) return;
        setAdding(true);
        try {
            await workApi.addStoryToSprint(selectedSprintId, { storyId: selectedStory.storyId });
            addToast('success', `Added "${selectedStory.storyKey}" to sprint`);
            setAddToSprintOpen(false);
            setSelectedSprintId('');
            fetchBacklog();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', 'Failed to add story to sprint');
            }
        } finally {
            setAdding(false);
        }
    };

    if (loading) return <SkeletonLoader variant="table" rows={8} columns={7} />;

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-semibold text-foreground">Backlog</h1>
                    {backlog && (
                        <p className="text-sm text-muted-foreground">
                            {backlog.totalStories} stories · {backlog.totalPoints} points
                        </p>
                    )}
                </div>
                <div className="flex items-center gap-2">
                    <SavedFilterDropdown onApply={(f) => setFilters(f as BoardFiltersType)} />
                    <SaveFilterDialog filters={filters} />
                    <BoardFilters filters={filters} onChange={setFilters} />
                </div>
            </div>

            {!backlog || backlog.items.length === 0 ? (
                <div className="py-12 text-center text-muted-foreground">No stories in backlog</div>
            ) : (
                <div className="overflow-x-auto rounded-md border border-border">
                    <table className="w-full text-sm">
                        <thead>
                            <tr className="border-b border-border bg-muted/50">
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Key</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Title</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Priority</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Points</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Assignee</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Labels</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Created</th>
                                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {backlog.items.map((item) => (
                                <tr
                                    key={item.storyId}
                                    className="border-b border-border last:border-0 cursor-pointer hover:bg-muted/50"
                                    onClick={() => navigate(`/stories/${item.storyId}`)}
                                >
                                    <td className="px-4 py-3 font-medium text-foreground">{item.storyKey}</td>
                                    <td className="px-4 py-3 text-foreground truncate max-w-xs">{item.title}</td>
                                    <td className="px-4 py-3"><Badge variant="priority" value={item.priority} /></td>
                                    <td className="px-4 py-3 text-foreground">{item.storyPoints ?? '—'}</td>
                                    <td className="px-4 py-3 text-muted-foreground">{item.assigneeName ?? 'Unassigned'}</td>
                                    <td className="px-4 py-3">
                                        <div className="flex gap-1">
                                            {item.labels.map((l) => (
                                                <span
                                                    key={l.labelId}
                                                    className="h-3 w-3 rounded-full"
                                                    style={{ backgroundColor: l.color }}
                                                    title={l.name}
                                                />
                                            ))}
                                        </div>
                                    </td>
                                    <td className="px-4 py-3 text-muted-foreground">{new Date(item.dateCreated).toLocaleDateString()}</td>
                                    <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                                        <button
                                            onClick={() => openAddToSprint(item)}
                                            className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-primary hover:bg-primary/10"
                                            title="Add to Sprint"
                                        >
                                            <Plus size={12} /> Sprint
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            {/* Add to Sprint Modal */}
            <Modal
                open={addToSprintOpen}
                onClose={() => { setAddToSprintOpen(false); setSelectedSprintId(''); }}
                title={`Add "${selectedStory?.storyKey}" to Sprint`}
            >
                <div className="space-y-4">
                    {sprints.length === 0 ? (
                        <p className="text-sm text-muted-foreground">No sprints in planning status available.</p>
                    ) : (
                        <>
                            <select
                                value={selectedSprintId}
                                onChange={(e) => setSelectedSprintId(e.target.value)}
                                className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground"
                            >
                                <option value="">Select a sprint</option>
                                {sprints.map((s) => (
                                    <option key={s.sprintId} value={s.sprintId}>
                                        {s.name} ({s.projectName})
                                    </option>
                                ))}
                            </select>
                            <div className="flex justify-end gap-2">
                                <button
                                    onClick={() => { setAddToSprintOpen(false); setSelectedSprintId(''); }}
                                    className="rounded-md border border-input px-3 py-2 text-sm font-medium text-foreground hover:bg-accent"
                                >
                                    Cancel
                                </button>
                                <button
                                    onClick={handleAddToSprint}
                                    disabled={!selectedSprintId || adding}
                                    className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                                >
                                    {adding ? 'Adding...' : 'Add to Sprint'}
                                </button>
                            </div>
                        </>
                    )}
                </div>
            </Modal>
        </div>
    );
}
