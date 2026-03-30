import { Priority, Role } from '@/types/enums';

type BadgeVariant = 'status' | 'priority' | 'role' | 'default';

interface BadgeProps {
    variant?: BadgeVariant;
    value: string;
    className?: string;
}

const statusColors: Record<string, string> = {
    Backlog: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
    Ready: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
    InProgress: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900 dark:text-yellow-300',
    InReview: 'bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300',
    QA: 'bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300',
    Done: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
    Closed: 'bg-gray-200 text-gray-600 dark:bg-gray-700 dark:text-gray-400',
    ToDo: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
    Planning: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
    Active: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
    Completed: 'bg-gray-200 text-gray-600 dark:bg-gray-700 dark:text-gray-400',
    Cancelled: 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
    Trialing: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
    PastDue: 'bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300',
    Expired: 'bg-gray-200 text-gray-600 dark:bg-gray-700 dark:text-gray-400',
};

const priorityColors: Record<string, string> = {
    [Priority.Critical]: 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
    [Priority.High]: 'bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300',
    [Priority.Medium]: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900 dark:text-yellow-300',
    [Priority.Low]: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
};

const roleColors: Record<string, string> = {
    [Role.OrgAdmin]: 'bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300',
    [Role.DeptLead]: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
    [Role.Member]: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
    [Role.Viewer]: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
};

function getColorClass(variant: BadgeVariant, value: string): string {
    const fallback = 'bg-secondary text-secondary-foreground';
    switch (variant) {
        case 'status':
            return statusColors[value] ?? fallback;
        case 'priority':
            return priorityColors[value] ?? fallback;
        case 'role':
            return roleColors[value] ?? fallback;
        default:
            return fallback;
    }
}

export function Badge({ variant = 'default', value, className = '' }: BadgeProps) {
    const color = getColorClass(variant, value);
    return (
        <span
            className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${color} ${className}`}
        >
            {value}
        </span>
    );
}
