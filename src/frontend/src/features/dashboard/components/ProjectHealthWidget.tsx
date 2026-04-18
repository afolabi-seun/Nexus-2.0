import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { analyticsApi } from '@/api/analyticsApi';
import type { ProjectHealthResponse } from '@/types/analytics';

interface ProjectHealth {
    projectId: string;
    name: string;
    health: ProjectHealthResponse | null;
}

function scoreColor(score: number): string {
    if (score > 70) return 'text-green-600';
    if (score >= 40) return 'text-yellow-500';
    return 'text-destructive';
}

export function ProjectHealthWidget() {
    const navigate = useNavigate();
    const [projects, setProjects] = useState<ProjectHealth[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        (async () => {
            try {
                const res = await workApi.getProjects({ page: 1, pageSize: 5 });
                const results = await Promise.all(
                    res.data.map(async (p) => ({
                        projectId: p.projectId,
                        name: p.name,
                        health: await analyticsApi.getProjectHealth({ projectId: p.projectId }).catch(() => null),
                    }))
                );
                setProjects(results);
            } catch {
                // non-critical
            } finally {
                setLoading(false);
            }
        })();
    }, []);

    if (loading) return <p className="text-sm text-muted-foreground">Loading…</p>;
    if (projects.length === 0) return <p className="text-sm text-muted-foreground">No projects yet.</p>;

    return (
        <div className="space-y-2">
            {projects.map((p) => (
                <div
                    key={p.projectId}
                    onClick={() => navigate(`/projects/${p.projectId}`)}
                    className="flex items-center justify-between rounded-md border border-border px-3 py-2 cursor-pointer hover:bg-accent"
                >
                    <span className="text-sm text-foreground truncate">{p.name}</span>
                    {p.health ? (
                        <div className="flex items-center gap-2">
                            <span className={`text-sm font-semibold ${scoreColor(p.health.overallScore)}`}>
                                {Math.round(p.health.overallScore)}
                            </span>
                            <span className="text-xs text-muted-foreground">{p.health.trend}</span>
                        </div>
                    ) : (
                        <span className="text-xs text-muted-foreground">No data</span>
                    )}
                </div>
            ))}
        </div>
    );
}
