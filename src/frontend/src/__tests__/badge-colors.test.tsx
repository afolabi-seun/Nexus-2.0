import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { Badge } from '@/components/common/Badge';
import { StoryStatus, TaskStatus, SprintStatus, Priority } from '@/types/enums';

/**
 * **Validates: Property 14; Requirements 12.7, 12.8**
 *
 * Unit tests for Badge color mapping:
 * - Status Badge renders correct colors for all StoryStatus values
 * - Priority Badge renders correct colors for all Priority values
 */

const expectedStatusColors: Record<string, string> = {
    Backlog: 'bg-gray-100',
    Ready: 'bg-blue-100',
    InProgress: 'bg-yellow-100',
    InReview: 'bg-purple-100',
    QA: 'bg-orange-100',
    Done: 'bg-green-100',
    Closed: 'bg-gray-200',
    ToDo: 'bg-gray-100',
    Planning: 'bg-blue-100',
    Active: 'bg-green-100',
    Completed: 'bg-gray-200',
    Cancelled: 'bg-red-100',
};

const expectedPriorityColors: Record<string, string> = {
    Critical: 'bg-red-100',
    High: 'bg-orange-100',
    Medium: 'bg-yellow-100',
    Low: 'bg-green-100',
};

describe('Badge Color Mapping', () => {
    describe('Status Badge', () => {
        Object.values(StoryStatus).forEach((status) => {
            it(`renders correct color for StoryStatus.${status}`, () => {
                const { container } = render(<Badge variant="status" value={status} />);
                const badge = container.querySelector('span')!;
                expect(badge.className).toContain(expectedStatusColors[status]);
                expect(badge.textContent).toBe(status);
            });
        });

        Object.values(TaskStatus).forEach((status) => {
            it(`renders correct color for TaskStatus.${status}`, () => {
                const { container } = render(<Badge variant="status" value={status} />);
                const badge = container.querySelector('span')!;
                expect(badge.className).toContain(expectedStatusColors[status]);
            });
        });

        Object.values(SprintStatus).forEach((status) => {
            it(`renders correct color for SprintStatus.${status}`, () => {
                const { container } = render(<Badge variant="status" value={status} />);
                const badge = container.querySelector('span')!;
                expect(badge.className).toContain(expectedStatusColors[status]);
            });
        });
    });

    describe('Priority Badge', () => {
        Object.values(Priority).forEach((priority) => {
            it(`renders correct color for Priority.${priority}`, () => {
                const { container } = render(<Badge variant="priority" value={priority} />);
                const badge = container.querySelector('span')!;
                expect(badge.className).toContain(expectedPriorityColors[priority]);
                expect(badge.textContent).toBe(priority);
            });
        });
    });

    describe('Default Badge', () => {
        it('renders fallback color for unknown status', () => {
            const { container } = render(<Badge variant="status" value="UnknownStatus" />);
            const badge = container.querySelector('span')!;
            expect(badge.className).toContain('bg-secondary');
        });

        it('renders fallback color for default variant', () => {
            const { container } = render(<Badge value="anything" />);
            const badge = container.querySelector('span')!;
            expect(badge.className).toContain('bg-secondary');
        });
    });
});
