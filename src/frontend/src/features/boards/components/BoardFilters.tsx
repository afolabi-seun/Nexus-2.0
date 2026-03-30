import { useState, useEffect } from 'react';
import { workApi } from '@/api/workApi';
import type { ProjectListItem } from '@/types/work';
import type { BoardFilters as BoardFiltersType } from '@/types/work';
import { Priority } from '@/types/enums';

interface BoardFiltersProps {
    filters: BoardFiltersType;
    onChange: (filters: BoardFiltersType) => void;
    showProject?: boolean;
}

export function BoardFilters({ filters, onChange, showProject = true }: BoardFiltersProps) {
    const [projects, setProjects] = useState<ProjectListItem[]>([]);

    useEffect(() => {
        workApi.getProjects({ page: 1, pageSize: 100 }).then((r) => setProjects(r.data)).catch(() => { });
    }, []);

    const selectClass = 'h-8 rounded-md border border-input bg-background px-2 text-sm text-foreground';

    return (
        <div className="flex flex-wrap items-center gap-2">
            {showProject && (
                <select
                    value={filters.projectId ?? ''}
                    onChange={(e) => onChange({ ...filters, projectId: e.target.value || undefined })}
                    className={selectClass}
                >
                    <option value="">All Projects</option>
                    {projects.map((p) => (
                        <option key={p.projectId} value={p.projectId}>{p.name}</option>
                    ))}
                </select>
            )}
            <select
                value={filters.priority ?? ''}
                onChange={(e) => onChange({ ...filters, priority: e.target.value || undefined })}
                className={selectClass}
            >
                <option value="">All Priorities</option>
                {Object.values(Priority).map((p) => (
                    <option key={p} value={p}>{p}</option>
                ))}
            </select>
        </div>
    );
}
