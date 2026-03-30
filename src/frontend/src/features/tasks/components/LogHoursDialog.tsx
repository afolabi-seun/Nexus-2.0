import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { workApi } from '@/api/workApi';
import { Modal } from '@/components/common/Modal';
import { FormField } from '@/components/forms/FormField';
import { useToast } from '@/components/common/Toast';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { logHoursSchema, type LogHoursFormData } from '../schemas';


interface LogHoursDialogProps {
    open: boolean;
    onClose: () => void;
    taskId: string;
    onSuccess: () => void;
}

export function LogHoursDialog({ open, onClose, taskId, onSuccess }: LogHoursDialogProps) {
    const { addToast } = useToast();
    const [submitting, setSubmitting] = useState(false);

    const {
        register,
        handleSubmit,
        control,
        reset,
        formState: { errors },
    } = useForm<LogHoursFormData>({
        resolver: zodResolver(logHoursSchema),
        defaultValues: { description: '' },
    });

    const onSubmit = async (data: LogHoursFormData) => {
        setSubmitting(true);
        try {
            await workApi.logHours(taskId, {
                hours: data.hours,
                description: data.description || undefined,
            });
            addToast('success', `${data.hours}h logged`);
            reset();
            onSuccess();
        } catch (err) {
            if (err instanceof ApiError) {
                addToast('error', mapErrorCode(err.errorCode));
            } else {
                addToast('error', 'Failed to log hours');
            }
        } finally {
            setSubmitting(false);
        }
    };

    const inputClass =
        'h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring';

    return (
        <Modal open={open} onClose={onClose} title="Log Hours">
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                <FormField name="hours" label="Hours" error={errors.hours?.message} required>
                    <Controller
                        name="hours"
                        control={control}
                        render={({ field }) => (
                            <input
                                id="hours"
                                type="number"
                                step="0.25"
                                min="0.25"
                                value={field.value ?? ''}
                                onChange={(e) => {
                                    const val = e.target.value;
                                    field.onChange(val ? Number(val) : undefined);
                                }}
                                className={inputClass}
                                placeholder="e.g. 2.5"
                            />
                        )}
                    />
                </FormField>

                <FormField name="description" label="Description" error={errors.description?.message}>
                    <textarea
                        id="description"
                        {...register('description')}
                        className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                        placeholder="What did you work on?"
                        rows={2}
                    />
                </FormField>

                <div className="flex justify-end gap-2 pt-2">
                    <button type="button" onClick={onClose} className="rounded-md border border-input px-3 py-2 text-sm font-medium text-foreground hover:bg-accent">
                        Cancel
                    </button>
                    <button
                        type="submit"
                        disabled={submitting}
                        className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                    >
                        {submitting ? 'Logging...' : 'Log Hours'}
                    </button>
                </div>
            </form>
        </Modal>
    );
}
