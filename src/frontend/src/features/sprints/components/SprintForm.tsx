import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { workApi } from '@/api/workApi';
import { FormField } from '@/components/forms/FormField';
import { useToast } from '@/components/common/Toast';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { createSprintSchema, type CreateSprintFormData } from '../schemas';
import type { ProjectListItem } from '@/types/work';

interface SprintFormProps {
    projects: ProjectListItem[];
    onSuccess: () => void;
}

export function SprintForm({ projects, onSuccess }: SprintFormProps) {
    const { addToast } = useToast();
    const [submitting, setSubmitting] = useState(false);
    const [serverError, setServerError] = useState('');
    const [selectedProjectId, setSelectedProjectId] = useState('');

    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm<CreateSprintFormData>({
        resolver: zodResolver(createSprintSchema),
        defaultValues: { name: '', goal: '', startDate: '', endDate: '' },
    });

    const onSubmit = async (data: CreateSprintFormData) => {
        if (!selectedProjectId) {
            setServerError('Please select a project');
            return;
        }
        setSubmitting(true);
        setServerError('');
        try {
            await workApi.createSprint(selectedProjectId, {
                name: data.name,
                goal: data.goal || undefined,
                startDate: data.startDate,
                endDate: data.endDate,
            });
            addToast('success', `Sprint "${data.name}" created`);
            onSuccess();
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

    const inputClass =
        'h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring';

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {serverError && (
                <p className="text-sm text-destructive" role="alert">{serverError}</p>
            )}

            <FormField name="projectId" label="Project" required>
                <select
                    id="projectId"
                    value={selectedProjectId}
                    onChange={(e) => setSelectedProjectId(e.target.value)}
                    className={inputClass}
                >
                    <option value="">Select a project</option>
                    {projects.map((p) => (
                        <option key={p.projectId} value={p.projectId}>{p.name} ({p.projectKey})</option>
                    ))}
                </select>
            </FormField>

            <FormField name="name" label="Sprint Name" error={errors.name?.message} required>
                <input id="name" {...register('name')} className={inputClass} placeholder="Sprint 1" maxLength={100} />
            </FormField>

            <FormField name="goal" label="Sprint Goal" error={errors.goal?.message}>
                <textarea
                    id="goal"
                    {...register('goal')}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    placeholder="What do you want to achieve?"
                    rows={2}
                />
            </FormField>

            <div className="grid grid-cols-2 gap-3">
                <FormField name="startDate" label="Start Date" error={errors.startDate?.message} required>
                    <input id="startDate" type="date" {...register('startDate')} className={inputClass} />
                </FormField>
                <FormField name="endDate" label="End Date" error={errors.endDate?.message} required>
                    <input id="endDate" type="date" {...register('endDate')} className={inputClass} />
                </FormField>
            </div>

            <div className="flex justify-end gap-2 pt-2">
                <button
                    type="submit"
                    disabled={submitting}
                    className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                >
                    {submitting ? 'Creating...' : 'Create Sprint'}
                </button>
            </div>
        </form>
    );
}
