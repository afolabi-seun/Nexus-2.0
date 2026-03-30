import { StoryStatus, TaskStatus } from '@/types/enums';

export const storyTransitions: Record<StoryStatus, StoryStatus[]> = {
    [StoryStatus.Backlog]: [StoryStatus.Ready],
    [StoryStatus.Ready]: [StoryStatus.InProgress],
    [StoryStatus.InProgress]: [StoryStatus.InReview],
    [StoryStatus.InReview]: [StoryStatus.QA, StoryStatus.InProgress],
    [StoryStatus.QA]: [StoryStatus.Done, StoryStatus.InProgress],
    [StoryStatus.Done]: [StoryStatus.Closed],
    [StoryStatus.Closed]: [],
};

export const taskTransitions: Record<TaskStatus, TaskStatus[]> = {
    [TaskStatus.ToDo]: [TaskStatus.InProgress],
    [TaskStatus.InProgress]: [TaskStatus.InReview],
    [TaskStatus.InReview]: [TaskStatus.InProgress, TaskStatus.Done],
    [TaskStatus.Done]: [],
};

export function getValidTransitions(
    entityType: 'Story' | 'Task',
    currentStatus: string
): string[] {
    if (entityType === 'Story') {
        return storyTransitions[currentStatus as StoryStatus] ?? [];
    }
    return taskTransitions[currentStatus as TaskStatus] ?? [];
}

export function isValidTransition(
    entityType: 'Story' | 'Task',
    from: string,
    to: string
): boolean {
    const valid = getValidTransitions(entityType, from);
    return valid.includes(to);
}
