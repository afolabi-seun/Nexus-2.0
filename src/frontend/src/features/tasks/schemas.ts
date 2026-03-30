import { z } from 'zod';
import { Priority, TaskType } from '@/types/enums';

export const createTaskSchema = z.object({
    storyId: z.string().uuid('Please select a story'),
    title: z
        .string()
        .min(1, 'Title is required')
        .max(200, 'Title must be 200 characters or fewer'),
    description: z.string().max(3000, 'Description must be 3000 characters or fewer').optional(),
    taskType: z.nativeEnum(TaskType, { message: 'Task type is required' }),
    priority: z.nativeEnum(Priority).optional(),
    estimatedHours: z
        .number()
        .positive('Estimated hours must be positive')
        .optional(),
    dueDate: z.string().optional(),
});

export type CreateTaskFormData = z.infer<typeof createTaskSchema>;

export const logHoursSchema = z.object({
    hours: z
        .number({ error: 'Hours is required' })
        .positive('Hours must be positive'),
    description: z.string().max(500).optional(),
});

export type LogHoursFormData = z.infer<typeof logHoursSchema>;
