import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { workApi } from '@/api/workApi';
import { FormField } from '@/components/forms/FormField';
import { useToast } from '@/components/common/Toast';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { Priority, TaskType } from '@/types/enums';
import { createTaskSchema, type CreateTaskFormData } from '../schemas';
import type { TaskDetail } from '@/types/work';

interface TaskFormProps {
    storyId: string;
    onSuccess: (task: TaskDetail) => void;
}

export function TaskForm({ storyId, onSuccess }: TaskFormProps) {
    const { addToast } = useToast();
    const [submitting, setSubmitting] = useState(false);
    const [serverError, setServerError] = useState('');

    const {
        register,
        handleSubmit,
        control,
        formState: { errors },
    } = useForm<CreateTaskFormData>({
        resolver: zodResolver(createTaskSchema),
        defaultValues: {
            storyId,
            title: '',
            description: '',
            taskType: TaskType.Development,
            priority: Priority.Medium,
            dueDate: '',
        },
    });

    const onSubmit = async (data: CreateTaskFormData) => {
        setSubmitting(true);
        setServerError('');
        try {
            const result = await workApi.createTask({
                storyId: data.storyId,
                title: data.title,
                description: data.description || undefined,
                taskType: data.taskType,
                priority: data.priority,
                estimatedHours: data.estimatedHours,
                dueDate: data.dueDate || undefined,
            });
            addToast('success', `Task "${result.title}" created`);
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

    const inputClass =
        'h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring';

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {serverError && (
                <p className="text-sm text-destructive" role="alert">{serverError}</p>
            )}

            <FormField name="title" label="Title" error={errors.title?.message} required>
                <input id="title" {...register('title')} className={inputClass} placeholder="Task title" maxLength={200} />
            </FormField>

            <FormField name="description" label="Description" error={errors.description?.message}>
                <textarea
                    id="description"
                    {...register('description')}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    placeholder="Describe the task..."
                    rows={3}
                    maxLength={3000}
                />
            </FormField>

            <div className="grid grid-cols-2 gap-3">
                <FormField name="taskType" label="Task Type" error={errors.taskType?.message} required>
                    <select id="taskType" {...register('taskType')} className={inputClass}>
                        {Object.values(TaskType).map((t) => (
                            <option key={t} value={t}>{t}</option>
                        ))}
                    </select>
                </FormField>

                <FormField name="priority" label="Priority" error={errors.priority?.message}>
                    <select id="priority" {...register('priority')} className={inputClass}>
                        {Object.values(Priority).map((p) => (
                            <option key={p} value={p}>{p}</option>
                        ))}
                    </select>
                </FormField>
            </div>

            <div className="grid grid-cols-2 gap-3">
                <FormField name="estimatedHours" label="Estimated Hours" error={errors.estimatedHours?.message}>
                    <Controller
                        name="estimatedHours"
                        control={control}
                        render={({ field }) => (
                            <input
                                id="estimatedHours"
                                type="number"
                                step="0.5"
                                min="0.5"
                                value={field.value ?? ''}
                                onChange={(e) => {
                                    const val = e.target.value;
                                    field.onChange(val ? Number(val) : undefined);
                                }}
                                className={inputClass}
                                placeholder="e.g. 4"
                            />
                        )}
                    />
                </FormField>

                <FormField name="dueDate" label="Due Date" error={errors.dueDate?.message}>
                    <input id="dueDate" type="date" {...register('dueDate')} className={inputClass} />
                </FormField>
            </div>

            <div className="flex justify-end gap-2 pt-2">
                <button
                    type="submit"
                    disabled={submitting}
                    className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                >
                    {submitting ? 'Creating...' : 'Create Task'}
                </button>
            </div>
        </form>
    );
}
