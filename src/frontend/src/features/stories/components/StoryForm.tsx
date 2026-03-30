import { useState, useEffect } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { workApi } from '@/api/workApi';
import { profileApi } from '@/api/profileApi';
import { FormField } from '@/components/forms/FormField';
import { MarkdownEditor } from '@/components/forms/MarkdownEditor';
import { useToast } from '@/components/common/Toast';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { Priority } from '@/types/enums';
import { createStorySchema, updateStorySchema, FIBONACCI_OPTIONS, type CreateStoryFormData, type UpdateStoryFormData } from '../schemas';
import type { StoryDetail, ProjectListItem, Label } from '@/types/work';
import type { Department } from '@/types/profile';

interface StoryFormCreateProps {
    mode?: 'create';
    story?: undefined;
    defaultProjectId?: string;
    onSuccess: (story: StoryDetail) => void;
}

interface StoryFormEditProps {
    mode: 'edit';
    story: StoryDetail;
    defaultProjectId?: string;
    onSuccess: (story: StoryDetail) => void;
}

type StoryFormProps = StoryFormCreateProps | StoryFormEditProps;

export function StoryForm({ mode = 'create', story, defaultProjectId, onSuccess }: StoryFormProps) {
    const { addToast } = useToast();
    const [submitting, setSubmitting] = useState(false);
    const [serverError, setServerError] = useState('');
    const [projects, setProjects] = useState<ProjectListItem[]>([]);
    const [departments, setDepartments] = useState<Department[]>([]);
    const [labels, setLabels] = useState<Label[]>([]);

    const isEdit = mode === 'edit';

    const {
        register,
        handleSubmit,
        control,
        formState: { errors },
    } = useForm<CreateStoryFormData | UpdateStoryFormData>({
        resolver: zodResolver(isEdit ? updateStorySchema : createStorySchema),
        defaultValues: isEdit && story
            ? {
                title: story.title,
                description: story.description ?? '',
                acceptanceCriteria: story.acceptanceCriteria ?? '',
                priority: story.priority,
                storyPoints: story.storyPoints ?? undefined,
                departmentId: story.departmentId ?? '',
                dueDate: story.dueDate?.split('T')[0] ?? '',
            }
            : {
                ...(isEdit ? {} : { projectId: defaultProjectId ?? '', labelIds: [] }),
                title: '',
                description: '',
                acceptanceCriteria: '',
                priority: Priority.Medium,
                departmentId: '',
                dueDate: '',
            },
    });

    useEffect(() => {
        Promise.all([
            workApi.getProjects({ page: 1, pageSize: 100 }),
            profileApi.getDepartments(),
            workApi.getLabels(),
        ]).then(([projRes, depts, lbls]) => {
            setProjects(projRes.data);
            setDepartments(depts);
            setLabels(lbls);
        }).catch(() => { });
    }, []);

    const onSubmit = async (data: CreateStoryFormData | UpdateStoryFormData) => {
        setSubmitting(true);
        setServerError('');
        try {
            let result: StoryDetail;
            if (isEdit && story) {
                const updateData = data as UpdateStoryFormData;
                result = await workApi.updateStory(story.storyId, {
                    title: updateData.title,
                    description: updateData.description || undefined,
                    acceptanceCriteria: updateData.acceptanceCriteria || undefined,
                    priority: updateData.priority,
                    storyPoints: updateData.storyPoints,
                    departmentId: updateData.departmentId || undefined,
                    dueDate: updateData.dueDate || undefined,
                });
                addToast('success', 'Story updated');
            } else {
                const createData = data as CreateStoryFormData;
                result = await workApi.createStory({
                    projectId: createData.projectId,
                    title: createData.title,
                    description: createData.description || undefined,
                    acceptanceCriteria: createData.acceptanceCriteria || undefined,
                    priority: createData.priority,
                    storyPoints: createData.storyPoints,
                    departmentId: createData.departmentId || undefined,
                    dueDate: createData.dueDate || undefined,
                    labelIds: createData.labelIds?.length ? createData.labelIds : undefined,
                });
                addToast('success', `Story "${result.storyKey}" created`);
            }
            onSuccess(result);
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

    const inputClass = 'h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring';

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 max-h-[70vh] overflow-y-auto pr-1">
            {serverError && (
                <p className="text-sm text-destructive" role="alert">{serverError}</p>
            )}

            {!isEdit && (
                <FormField name="projectId" label="Project" error={(errors as Record<string, { message?: string }>).projectId?.message} required>
                    <select id="projectId" {...register('projectId' as 'title')} className={inputClass}>
                        <option value="">Select a project</option>
                        {projects.map((p) => (
                            <option key={p.projectId} value={p.projectId}>{p.name} ({p.projectKey})</option>
                        ))}
                    </select>
                </FormField>
            )}

            <FormField name="title" label="Title" error={errors.title?.message} required>
                <input id="title" {...register('title')} className={inputClass} placeholder="Story title" maxLength={200} />
            </FormField>

            <FormField name="description" label="Description" error={errors.description?.message}>
                <Controller
                    name="description"
                    control={control}
                    render={({ field }) => (
                        <MarkdownEditor
                            id="description"
                            value={field.value ?? ''}
                            onChange={field.onChange}
                            placeholder="Describe the story..."
                            rows={4}
                        />
                    )}
                />
            </FormField>

            <FormField name="acceptanceCriteria" label="Acceptance Criteria" error={(errors as Record<string, { message?: string }>).acceptanceCriteria?.message}>
                <Controller
                    name="acceptanceCriteria"
                    control={control}
                    render={({ field }) => (
                        <MarkdownEditor
                            id="acceptanceCriteria"
                            value={field.value ?? ''}
                            onChange={field.onChange}
                            placeholder="Define acceptance criteria..."
                            rows={3}
                        />
                    )}
                />
            </FormField>

            <div className="grid grid-cols-2 gap-3">
                <FormField name="priority" label="Priority" error={errors.priority?.message}>
                    <select id="priority" {...register('priority')} className={inputClass}>
                        {Object.values(Priority).map((p) => (
                            <option key={p} value={p}>{p}</option>
                        ))}
                    </select>
                </FormField>

                <FormField name="storyPoints" label="Story Points" error={errors.storyPoints?.message}>
                    <Controller
                        name="storyPoints"
                        control={control}
                        render={({ field }) => (
                            <select
                                id="storyPoints"
                                value={String(field.value ?? '')}
                                onChange={(e) => {
                                    const val = e.target.value;
                                    field.onChange(val ? Number(val) : undefined);
                                }}
                                className={inputClass}
                            >
                                <option value="">None</option>
                                {FIBONACCI_OPTIONS.map((o) => (
                                    <option key={o.value} value={o.value}>{o.label}</option>
                                ))}
                            </select>
                        )}
                    />
                </FormField>
            </div>

            <div className="grid grid-cols-2 gap-3">
                <FormField name="departmentId" label="Department" error={(errors as Record<string, { message?: string }>).departmentId?.message}>
                    <select id="departmentId" {...register('departmentId' as 'title')} className={inputClass}>
                        <option value="">None</option>
                        {departments.map((d) => (
                            <option key={d.departmentId} value={d.departmentId}>{d.name}</option>
                        ))}
                    </select>
                </FormField>

                <FormField name="dueDate" label="Due Date" error={errors.dueDate?.message}>
                    <input id="dueDate" type="date" {...register('dueDate')} className={inputClass} />
                </FormField>
            </div>

            {!isEdit && (
                <FormField name="labelIds" label="Labels">
                    <div className="flex flex-wrap gap-1.5">
                        {labels.map((l) => (
                            <Controller
                                key={l.labelId}
                                name={'labelIds' as 'title'}
                                control={control}
                                render={({ field }) => {
                                    const selected = ((field.value as unknown as string[]) ?? []).includes(l.labelId);
                                    return (
                                        <button
                                            type="button"
                                            onClick={() => {
                                                const current = (field.value as unknown as string[]) ?? [];
                                                const next = selected
                                                    ? current.filter((id) => id !== l.labelId)
                                                    : [...current, l.labelId];
                                                field.onChange(next);
                                            }}
                                            className={`rounded-full px-2 py-0.5 text-xs font-medium border transition-colors ${selected
                                                ? 'text-white border-transparent'
                                                : 'text-muted-foreground border-input hover:bg-accent'
                                                }`}
                                            style={selected ? { backgroundColor: l.color } : undefined}
                                        >
                                            {l.name}
                                        </button>
                                    );
                                }}
                            />
                        ))}
                    </div>
                </FormField>
            )}

            <div className="flex justify-end gap-2 pt-2">
                <button
                    type="submit"
                    disabled={submitting}
                    className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                >
                    {submitting ? 'Saving...' : isEdit ? 'Save Changes' : 'Create Story'}
                </button>
            </div>
        </form>
    );
}
