import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { createProjectSchema, type CreateProjectFormData } from '../schemas';
import { FormField } from '@/components/forms/FormField';
import { MemberSelector } from '@/components/forms/MemberSelector';

interface ProjectFormProps {
    onSubmit: (data: CreateProjectFormData) => Promise<void>;
    submitting?: boolean;
    serverError?: string;
}

export function ProjectForm({ onSubmit, submitting, serverError }: ProjectFormProps) {
    const {
        register,
        handleSubmit,
        setValue,
        formState: { errors },
    } = useForm<CreateProjectFormData>({
        resolver: zodResolver(createProjectSchema),
        defaultValues: { name: '', projectKey: '', description: '', leadId: '' },
    });

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {serverError && (
                <p className="text-sm text-destructive" role="alert">{serverError}</p>
            )}

            <FormField name="name" label="Project Name" error={errors.name?.message} required>
                <input
                    id="name"
                    {...register('name')}
                    className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    placeholder="My Project"
                />
            </FormField>

            <FormField name="projectKey" label="Project Key" error={errors.projectKey?.message} required>
                <input
                    id="projectKey"
                    {...register('projectKey')}
                    className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm uppercase text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    placeholder="PROJ"
                    maxLength={10}
                />
            </FormField>

            <FormField name="description" label="Description" error={errors.description?.message}>
                <textarea
                    id="description"
                    {...register('description')}
                    rows={3}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    placeholder="Optional project description"
                />
            </FormField>

            <FormField name="leadId" label="Project Lead" error={errors.leadId?.message}>
                <MemberSelector
                    onSelect={(memberId) => setValue('leadId', memberId, { shouldValidate: true })}
                    placeholder="Search for a lead..."
                />
            </FormField>

            <div className="flex justify-end gap-2 pt-2">
                <button
                    type="submit"
                    disabled={submitting}
                    className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                >
                    {submitting ? 'Creating...' : 'Create Project'}
                </button>
            </div>
        </form>
    );
}
