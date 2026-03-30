import { z } from 'zod';

export const createSprintSchema = z
    .object({
        name: z.string().min(1, 'Name is required').max(100, 'Name must be 100 characters or fewer'),
        goal: z.string().max(500).optional(),
        startDate: z.string().min(1, 'Start date is required'),
        endDate: z.string().min(1, 'End date is required'),
    })
    .refine((data) => !data.startDate || !data.endDate || new Date(data.endDate) > new Date(data.startDate), {
        message: 'End date must be after start date',
        path: ['endDate'],
    });

export type CreateSprintFormData = z.infer<typeof createSprintSchema>;
