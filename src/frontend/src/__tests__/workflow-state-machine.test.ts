import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import {
    getValidTransitions,
    isValidTransition,
    storyTransitions,
    taskTransitions,
} from '@/utils/workflowStateMachine';
import { StoryStatus, TaskStatus } from '@/types/enums';

/**
 * **Validates: Requirements 13.11, 17.4**
 *
 * Property 15: Workflow state machine valid transitions
 * For any entity type and status, getValidTransitions returns only defined transitions;
 * isValidTransition is correct.
 */

const arbStoryStatus = fc.constantFrom(...Object.values(StoryStatus));
const arbTaskStatus = fc.constantFrom(...Object.values(TaskStatus));

describe('Workflow State Machine', () => {
    it('property: getValidTransitions for Story returns defined transitions', () => {
        fc.assert(
            fc.property(arbStoryStatus, (status) => {
                const transitions = getValidTransitions('Story', status);
                const expected = storyTransitions[status] ?? [];
                expect(transitions).toEqual(expected);
            }),
            { numRuns: 50 }
        );
    });

    it('property: getValidTransitions for Task returns defined transitions', () => {
        fc.assert(
            fc.property(arbTaskStatus, (status) => {
                const transitions = getValidTransitions('Task', status);
                const expected = taskTransitions[status] ?? [];
                expect(transitions).toEqual(expected);
            }),
            { numRuns: 50 }
        );
    });

    it('property: isValidTransition returns true iff target is in getValidTransitions (Story)', () => {
        fc.assert(
            fc.property(arbStoryStatus, arbStoryStatus, (from, to) => {
                const valid = getValidTransitions('Story', from);
                expect(isValidTransition('Story', from, to)).toBe(valid.includes(to));
            }),
            { numRuns: 100 }
        );
    });

    it('property: isValidTransition returns true iff target is in getValidTransitions (Task)', () => {
        fc.assert(
            fc.property(arbTaskStatus, arbTaskStatus, (from, to) => {
                const valid = getValidTransitions('Task', from);
                expect(isValidTransition('Task', from, to)).toBe(valid.includes(to));
            }),
            { numRuns: 100 }
        );
    });

    it('property: unknown status returns empty transitions', () => {
        fc.assert(
            fc.property(
                fc.constantFrom('Story', 'Task') as fc.Arbitrary<'Story' | 'Task'>,
                fc.stringMatching(/^[A-Z][a-z]{5,10}$/).filter(
                    (s) => !Object.values(StoryStatus).includes(s as StoryStatus) &&
                        !Object.values(TaskStatus).includes(s as TaskStatus)
                ),
                (entityType, unknownStatus) => {
                    const transitions = getValidTransitions(entityType, unknownStatus);
                    expect(transitions).toEqual([]);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: Closed story has no valid transitions', () => {
        const transitions = getValidTransitions('Story', StoryStatus.Closed);
        expect(transitions).toEqual([]);
    });

    it('property: Done task has no valid transitions', () => {
        const transitions = getValidTransitions('Task', TaskStatus.Done);
        expect(transitions).toEqual([]);
    });
});
