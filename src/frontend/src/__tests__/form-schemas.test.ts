import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { createStorySchema } from '@/features/stories/schemas';
import { createTaskSchema } from '@/features/tasks/schemas';
import { createSprintSchema } from '@/features/sprints/schemas';
import { createProjectSchema } from '@/features/projects/schemas';
import { Priority, TaskType } from '@/types/enums';

/**
 * **Validates: Requirements 14.5, 17.11, 18.9, 11.7, 28.7, 31.6**
 *
 * Property 16: Zod form validation schemas accept valid and reject invalid inputs
 * For all form schemas, valid inputs parse, invalid inputs produce errors.
 */

const arbUuid = fc.uuid();
const FIBONACCI_POINTS = [1, 2, 3, 5, 8, 13, 21];

describe('Form Validation Schemas', () => {
    describe('createStorySchema', () => {
        it('property: accepts valid story data', () => {
            fc.assert(
                fc.property(
                    fc.record({
                        projectId: arbUuid,
                        title: fc.string({ minLength: 1, maxLength: 200 }),
                        priority: fc.constantFrom(...Object.values(Priority)),
                        storyPoints: fc.constantFrom(...FIBONACCI_POINTS),
                    }),
                    ({ projectId, title, priority, storyPoints }) => {
                        const result = createStorySchema.safeParse({
                            projectId,
                            title,
                            priority,
                            storyPoints,
                        });
                        expect(result.success).toBe(true);
                    }
                ),
                { numRuns: 50 }
            );
        });

        it('property: rejects empty title', () => {
            fc.assert(
                fc.property(arbUuid, (projectId) => {
                    const result = createStorySchema.safeParse({
                        projectId,
                        title: '',
                    });
                    expect(result.success).toBe(false);
                }),
                { numRuns: 20 }
            );
        });

        it('property: rejects non-Fibonacci story points', () => {
            fc.assert(
                fc.property(
                    arbUuid,
                    fc.string({ minLength: 1, maxLength: 50 }),
                    fc.integer({ min: 1, max: 100 }).filter((n) => !FIBONACCI_POINTS.includes(n)),
                    (projectId, title, points) => {
                        const result = createStorySchema.safeParse({
                            projectId,
                            title,
                            storyPoints: points,
                        });
                        expect(result.success).toBe(false);
                    }
                ),
                { numRuns: 50 }
            );
        });
    });

    describe('createTaskSchema', () => {
        it('property: accepts valid task data', () => {
            fc.assert(
                fc.property(
                    fc.record({
                        storyId: arbUuid,
                        title: fc.string({ minLength: 1, maxLength: 200 }),
                        taskType: fc.constantFrom(...Object.values(TaskType)),
                    }),
                    ({ storyId, title, taskType }) => {
                        const result = createTaskSchema.safeParse({
                            storyId,
                            title,
                            taskType,
                        });
                        expect(result.success).toBe(true);
                    }
                ),
                { numRuns: 50 }
            );
        });

        it('property: rejects empty title', () => {
            fc.assert(
                fc.property(
                    arbUuid,
                    fc.constantFrom(...Object.values(TaskType)),
                    (storyId, taskType) => {
                        const result = createTaskSchema.safeParse({
                            storyId,
                            title: '',
                            taskType,
                        });
                        expect(result.success).toBe(false);
                    }
                ),
                { numRuns: 20 }
            );
        });

        it('property: rejects negative estimated hours', () => {
            fc.assert(
                fc.property(
                    arbUuid,
                    fc.string({ minLength: 1, maxLength: 50 }),
                    fc.constantFrom(...Object.values(TaskType)),
                    fc.double({ min: -1000, max: 0, noNaN: true }),
                    (storyId, title, taskType, hours) => {
                        const result = createTaskSchema.safeParse({
                            storyId,
                            title,
                            taskType,
                            estimatedHours: hours,
                        });
                        expect(result.success).toBe(false);
                    }
                ),
                { numRuns: 50 }
            );
        });
    });

    describe('createSprintSchema', () => {
        it('property: accepts valid sprint data with end after start', () => {
            fc.assert(
                fc.property(
                    fc.string({ minLength: 1, maxLength: 100 }),
                    fc.integer({ min: 0, max: 700 }),
                    fc.integer({ min: 1, max: 30 }),
                    (name, dayOffset, daysToAdd) => {
                        const base = new Date('2024-01-01');
                        const startDate = new Date(base.getTime() + dayOffset * 86400000);
                        const endDate = new Date(startDate.getTime() + daysToAdd * 86400000);

                        const result = createSprintSchema.safeParse({
                            name,
                            startDate: startDate.toISOString().split('T')[0],
                            endDate: endDate.toISOString().split('T')[0],
                        });
                        expect(result.success).toBe(true);
                    }
                ),
                { numRuns: 50 }
            );
        });

        it('property: rejects sprint with end date before start date', () => {
            fc.assert(
                fc.property(
                    fc.string({ minLength: 1, maxLength: 100 }),
                    fc.integer({ min: 150, max: 700 }),
                    fc.integer({ min: 1, max: 30 }),
                    (name, dayOffset, daysToSubtract) => {
                        const base = new Date('2024-01-01');
                        const endDate = new Date(base.getTime() + dayOffset * 86400000);
                        const startDate = new Date(endDate.getTime() + daysToSubtract * 86400000);

                        const result = createSprintSchema.safeParse({
                            name,
                            startDate: startDate.toISOString().split('T')[0],
                            endDate: endDate.toISOString().split('T')[0],
                        });
                        expect(result.success).toBe(false);
                    }
                ),
                { numRuns: 50 }
            );
        });
    });

    describe('createProjectSchema', () => {
        it('property: accepts valid project data', () => {
            fc.assert(
                fc.property(
                    fc.string({ minLength: 1, maxLength: 100 }),
                    fc.stringMatching(/^[A-Z0-9]{2,10}$/),
                    (name, projectKey) => {
                        const result = createProjectSchema.safeParse({ name, projectKey });
                        expect(result.success).toBe(true);
                    }
                ),
                { numRuns: 50 }
            );
        });

        it('property: rejects invalid project key format', () => {
            fc.assert(
                fc.property(
                    fc.string({ minLength: 1, maxLength: 100 }),
                    fc.stringMatching(/^[a-z]{2,10}$/),
                    (name, projectKey) => {
                        const result = createProjectSchema.safeParse({ name, projectKey });
                        expect(result.success).toBe(false);
                    }
                ),
                { numRuns: 50 }
            );
        });

        it('property: rejects empty project name', () => {
            fc.assert(
                fc.property(
                    fc.stringMatching(/^[A-Z0-9]{2,10}$/),
                    (projectKey) => {
                        const result = createProjectSchema.safeParse({ name: '', projectKey });
                        expect(result.success).toBe(false);
                    }
                ),
                { numRuns: 20 }
            );
        });
    });
});
