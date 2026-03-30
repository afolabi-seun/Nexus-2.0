import { z } from 'zod';

export const createProjectSchema = z.object({
    name: z.string().min(1, 'Project name is required'),
    projectKey: z
        .string()
        .min(1, 'Project key is required')
        .regex(/^[A-Z0-9]{2,10}$/, 'Must be 2–10 uppercase alphanumeric characters'),
    description: z.string().optional(),
    leadId: z
        .string()
        .uuid('Invalid lead selection')
        .optional()
        .or(z.literal('')),
});

export type CreateProjectFormData = z.infer<typeof createProjectSchema>;

export const updateProjectSchema = z.object({
    name: z.string().min(1, 'Project name is required'),
    description: z.string().optional(),
    leadId: z
        .string()
        .uuid('Invalid lead selection')
        .optional()
        .or(z.literal('')),
});

export type UpdateProjectFormData = z.infer<typeof updateProjectSchema>;
