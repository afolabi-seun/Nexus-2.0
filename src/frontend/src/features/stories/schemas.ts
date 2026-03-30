import { z } from 'zod';
import { Priority } from '@/types/enums';

const FIBONACCI_POINTS = [1, 2, 3, 5, 8, 13, 21] as const;

export const createStorySchema = z.object({
    projectId: z.string().uuid('Please select a project'),
    title: z
        .string()
        .min(1, 'Title is required')
        .max(200, 'Title must be 200 characters or fewer'),
    description: z.string().max(5000, 'Description must be 5000 characters or fewer').optional(),
    acceptanceCriteria: z.string().optional(),
    priority: z.nativeEnum(Priority).optional(),
    storyPoints: z
        .number()
        .refine((v) => FIBONACCI_POINTS.includes(v as (typeof FIBONACCI_POINTS)[number]), {
            message: 'Story points must be a Fibonacci number (1, 2, 3, 5, 8, 13, 21)',
        })
        .optional(),
    departmentId: z.string().uuid().optional().or(z.literal('')),
    dueDate: z.string().optional(),
    labelIds: z.array(z.string().uuid()).optional(),
});

export type CreateStoryFormData = z.infer<typeof createStorySchema>;

export const updateStorySchema = z.object({
    title: z
        .string()
        .min(1, 'Title is required')
        .max(200, 'Title must be 200 characters or fewer'),
    description: z.string().max(5000, 'Description must be 5000 characters or fewer').optional(),
    acceptanceCriteria: z.string().optional(),
    priority: z.nativeEnum(Priority).optional(),
    storyPoints: z
        .number()
        .refine((v) => FIBONACCI_POINTS.includes(v as (typeof FIBONACCI_POINTS)[number]), {
            message: 'Story points must be a Fibonacci number (1, 2, 3, 5, 8, 13, 21)',
        })
        .optional(),
    departmentId: z.string().uuid().optional().or(z.literal('')),
    dueDate: z.string().optional(),
});

export type UpdateStoryFormData = z.infer<typeof updateStorySchema>;

export const FIBONACCI_OPTIONS = FIBONACCI_POINTS.map((v) => ({ value: v, label: String(v) }));
