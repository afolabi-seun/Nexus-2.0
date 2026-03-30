import { useState, useEffect, useCallback } from 'react';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { Badge } from '@/components/common/Badge';
import type { DepartmentBoard, BoardFilters as BoardFiltersType } from '@/types/work';
import { BoardFilters } from '../components/BoardFilters.js';
import { SaveFilterDialog } from '@/features/filters/components/SaveFilterDialog';
import { SavedFilterDropdown } from '@/features/filters/components/SavedFilterDropdown';

export function DepartmentBoardPage() {
    const { addToast } = useToast();
    const [board, setBoard] = useState<DepartmentBoard | null>(null);
    const [loading, setLoading] = useState(true);
    const [filters, setFilters] = useState<BoardFiltersType>({});

    const fetchBoard = useCallback(async () => {
        setLoading(true);
        try {
            const data = await workApi.getDepartmentBoard(filters);
            setBoard(data);
        } catch {
            addToast('error', 'Failed to load department board');
        } finally {
            setLoading(false);
        }
    }, [filters, addToast]);

    useEffect(() => { fetchBoard(); }, [fetchBoard]);

    if (loading) return <SkeletonLoader variant="table" rows={5} columns={4} />;

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-semibold text-foreground">Department Board</h1>
                <div className="flex items-center gap-2">
                    <SavedFilterDropdown onApply={(f) => setFilters(f as BoardFiltersType)} />
                    <SaveFilterDialog filters={filters} />
                    <BoardFilters filters={filters} onChange={setFilters} />
                </div>
            </div>

            {!board || board.departments.length === 0 ? (
                <div className="py-12 text-center text-muted-foreground">No department data available</div>
            ) : (
                <div className="flex gap-3 overflow-x-auto pb-4">
                    {board.departments.map((dept) => (
                        <div
                            key={dept.departmentName}
                            className="flex min-w-[280px] flex-col rounded-lg border-2 border-border bg-muted/30"
                        >
                            <div className="border-b border-border px-3 py-2">
                                <div className="flex items-center justify-between">
                                    <h3 className="text-sm font-semibold text-foreground">{dept.departmentName}</h3>
                                    <span className="rounded-full bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">
                                        {dept.taskCount} tasks
                                    </span>
                                </div>
                                <p className="text-[10px] text-muted-foreground">{dept.memberCount} members</p>
                                {/* Workload indicator */}
                                {dept.taskCount > 0 && dept.memberCount > 0 && (
                                    <div className="mt-1">
                                        <WorkloadIndicator tasksPerMember={dept.taskCount / dept.memberCount} />
                                    </div>
                                )}
                            </div>
                            <div className="flex-1 space-y-1.5 overflow-y-auto p-2" style={{ maxHeight: 'calc(100vh - 240px)' }}>
                                {Object.entries(dept.tasksByStatus).map(([status, count]) => (
                                    <div key={status} className="flex items-center justify-between rounded-md border border-border bg-card px-3 py-2">
                                        <Badge variant="status" value={status} />
                                        <span className="text-sm font-medium text-foreground">{count}</span>
                                    </div>
                                ))}
                                {Object.keys(dept.tasksByStatus).length === 0 && (
                                    <p className="py-4 text-center text-xs text-muted-foreground">No tasks</p>
                                )}
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

function WorkloadIndicator({ tasksPerMember }: { tasksPerMember: number }) {
    const level = tasksPerMember > 5 ? 'High' : tasksPerMember > 2 ? 'Medium' : 'Low';
    const colors: Record<string, string> = {
        Low: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
        Medium: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900 dark:text-yellow-300',
        High: 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
    };
    return (
        <span className={`inline-flex items-center rounded-full px-1.5 py-0.5 text-[10px] font-medium ${colors[level]}`}>
            {level} workload
        </span>
    );
}
