import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { workApi } from '@/api/workApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { FormField } from '@/components/forms/FormField';
import { MemberSelector } from '@/components/forms/MemberSelector';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { ProjectDetail, StoryListItem, SprintListItem } from '@/types/work';
import { updateProjectSchema, type UpdateProjectFormData } from '../schemas';
import { ArrowLeft, Pencil } from 'lucide-react';

export function ProjectDetailPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { addToast } = useToast();

    const [project, setProject] = useState<ProjectDetail | null>(null);
    const [stories, setStories] = useState<StoryListItem[]>([]);
    const [sprints, setSprints] = useState<SprintListItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [editOpen, setEditOpen] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [serverError, setServerError] = useState('');

    const {
        register,
        handleSubmit,
        setValue,
        reset,
        formState: { errors },
    } = useForm<UpdateProjectFormData>({
        resolver: zodResolver(updateProjectSchema),
    });

    const fetchProject = useCallback(async () => {
        if (!id) return;
        setLoading(true);
        try {
            const [proj, storyRes, sprintRes] = await Promise.all([
                workApi.getProject(id),
                workApi.getStories({ projectId: id, page: 1, pageSize: 10 }),
                workApi.getSprints({ projectId: id, page: 1, pageSize: 10 }),
            ]);
            setProject(proj);
            setStories(storyRes.data);
            setSprints(sprintRes.data);
        } catch {
            addToast('error', 'Failed to load project');
        } finally {
            setLoading(false);
        }
    }, [id, addToast]);

    useEffect(() => {
        fetchProject();
    }, [fetchProject]);

    const openEdit = () => {
        if (!project) return;
        reset({
            name: project.name,
            description: project.description ?? '',
            leadId: project.leadId ?? '',
        });
        setServerError('');
        setEditOpen(true);
    };

    const handleUpdate = async (data: UpdateProjectFormData) => {
        if (!id) return;
        setSubmitting(true);
        setServerError('');
        try {
            const updated = await workApi.updateProject(id, {
                name: data.name,
                description: data.description || undefined,
                leadId: data.leadId || undefined,
            });
            setProject(updated);
            addToast('success', 'Project updated');
            setEditOpen(false);
        } catch (err) {
            if (err instanceof ApiError) {
                setServerError(mapErrorCode(err.errorCode));
            } else {
                setServerError('Something went wrong. Please try again.');
            }
        } finally {
            setSubmitting(false);
        }
    };

    const storyColumns: Column<StoryListItem>[] = [
        { key: 'storyKey', header: 'Key' },
        { key: 'title', header: 'Title' },
        {
            key: 'status',
            header: 'Status',
            render: (row) => <Badge variant="status" value={row.status} />,
        },
        {
            key: 'priority',
            header: 'Priority',
            render: (row) => <Badge variant="priority" value={row.priority} />,
        },
        {
            key: 'assigneeName',
            header: 'Assignee',
            render: (row) => row.assigneeName ?? '—',
        },
    ];

    const sprintColumns: Column<SprintListItem>[] = [
        { key: 'name', header: 'Sprint' },
        {
            key: 'status',
            header: 'Status',
            render: (row) => <Badge variant="status" value={row.status} />,
        },
        { key: 'storyCount', header: 'Stories' },
        { key: 'startDate', header: 'Start', render: (row) => new Date(row.startDate).toLocaleDateString() },
        { key: 'endDate', header: 'End', render: (row) => new Date(row.endDate).toLocaleDateString() },
    ];

    if (loading) {
        return <SkeletonLoader variant="form" />;
    }

    if (!project) {
        return (
            <div className="py-12 text-center text-muted-foreground">
                Project not found
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-center gap-3">
                <button
                    onClick={() => navigate('/projects')}
                    className="rounded-md p-1.5 text-muted-foreground hover:bg-accent"
                    aria-label="Back to projects"
                >
                    <ArrowLeft size={18} />
                </button>
                <div className="flex-1">
                    <div className="flex items-center gap-3">
                        <h1 className="text-2xl font-semibold text-foreground">{project.name}</h1>
                        <Badge variant="status" value={project.status} />
                    </div>
                    <p className="text-sm text-muted-foreground">
                        {project.projectKey} · Lead: {project.leadName ?? 'Unassigned'}
                    </p>
                </div>
                <button
                    onClick={openEdit}
                    className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-2 text-sm font-medium text-foreground hover:bg-accent"
                >
                    <Pencil size={14} />
                    Edit
                </button>
            </div>

            {project.description && (
                <p className="text-sm text-muted-foreground">{project.description}</p>
            )}

            {/* Stats */}
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
                <StatCard label="Stories" value={project.storyCount} />
                <StatCard label="Sprints" value={project.sprintCount} />
                <StatCard label="Key" value={project.projectKey} />
                <StatCard label="Status" value={project.status} />
            </div>

            {/* Stories */}
            <section className="space-y-2">
                <h2 className="text-lg font-medium text-foreground">Stories</h2>
                <DataTable
                    columns={storyColumns}
                    data={stories}
                    onRowClick={(row) => navigate(`/stories/${row.storyId}`)}
                    keyExtractor={(row) => row.storyId}
                />
            </section>

            {/* Sprints */}
            <section className="space-y-2">
                <h2 className="text-lg font-medium text-foreground">Sprints</h2>
                <DataTable
                    columns={sprintColumns}
                    data={sprints}
                    onRowClick={(row) => navigate(`/sprints/${row.sprintId}`)}
                    keyExtractor={(row) => row.sprintId}
                />
            </section>

            {/* Edit Modal */}
            <Modal open={editOpen} onClose={() => setEditOpen(false)} title="Edit Project">
                <form onSubmit={handleSubmit(handleUpdate)} className="space-y-4">
                    {serverError && (
                        <p className="text-sm text-destructive" role="alert">{serverError}</p>
                    )}
                    <FormField name="name" label="Project Name" error={errors.name?.message} required>
                        <input
                            id="name"
                            {...register('name')}
                            className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                        />
                    </FormField>
                    <FormField name="description" label="Description" error={errors.description?.message}>
                        <textarea
                            id="description"
                            {...register('description')}
                            rows={3}
                            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                        />
                    </FormField>
                    <FormField name="leadId" label="Project Lead" error={errors.leadId?.message}>
                        <MemberSelector
                            value={project.leadId ?? undefined}
                            onSelect={(memberId) => setValue('leadId', memberId, { shouldValidate: true })}
                            placeholder="Search for a lead..."
                        />
                    </FormField>
                    <div className="flex justify-end gap-2 pt-2">
                        <button
                            type="button"
                            onClick={() => setEditOpen(false)}
                            className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent"
                        >
                            Cancel
                        </button>
                        <button
                            type="submit"
                            disabled={submitting}
                            className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                        >
                            {submitting ? 'Saving...' : 'Save Changes'}
                        </button>
                    </div>
                </form>
            </Modal>
        </div>
    );
}

function StatCard({ label, value }: { label: string; value: string | number }) {
    return (
        <div className="rounded-lg border border-border bg-card p-4">
            <p className="text-xs text-muted-foreground">{label}</p>
            <p className="mt-1 text-lg font-semibold text-card-foreground">{value}</p>
        </div>
    );
}
