import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Pagination } from '@/components/common/Pagination';
import { Badge } from '@/components/common/Badge';
import { Modal } from '@/components/common/Modal';
import { useToast } from '@/components/common/Toast';
import { usePagination } from '@/hooks/usePagination';
import { useAuth } from '@/hooks/useAuth';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { ProjectListItem } from '@/types/work';
import type { CreateProjectFormData } from '../schemas';
import { ProjectForm } from '../components/ProjectForm.js';
import { Plus } from 'lucide-react';

export function ProjectListPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const { user } = useAuth();
    const { page, pageSize, setPage, setPageSize } = usePagination();

    const [projects, setProjects] = useState<ProjectListItem[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [loading, setLoading] = useState(true);
    const [sortBy, setSortBy] = useState<string | undefined>();
    const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
    const [modalOpen, setModalOpen] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [serverError, setServerError] = useState('');

    const canCreate = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead';

    const fetchProjects = useCallback(async () => {
        setLoading(true);
        try {
            const res = await workApi.getProjects({ page, pageSize, sortBy, sortDirection });
            setProjects(res.data);
            setTotalCount(res.totalCount);
        } catch {
            addToast('error', 'Failed to load projects');
        } finally {
            setLoading(false);
        }
    }, [page, pageSize, sortBy, sortDirection, addToast]);

    useEffect(() => {
        fetchProjects();
    }, [fetchProjects]);

    const handleSort = (key: string) => {
        if (sortBy === key) {
            setSortDirection((d) => (d === 'asc' ? 'desc' : 'asc'));
        } else {
            setSortBy(key);
            setSortDirection('asc');
        }
    };

    const handleCreate = async (data: CreateProjectFormData) => {
        setSubmitting(true);
        setServerError('');
        try {
            const created = await workApi.createProject({
                name: data.name,
                projectKey: data.projectKey,
                description: data.description || undefined,
                leadId: data.leadId || undefined,
            });
            addToast('success', `Project "${created.name}" created`);
            setModalOpen(false);
            fetchProjects();
        } catch (err) {
            if (err instanceof ApiError) {
                setServerError(mapErrorCode(err.errorCode));
            } else {
                setServerError('Something went wrong. Please try again.');
            }
        } finally {
            setSubmitting(false);
        }
    };

    const columns: Column<ProjectListItem>[] = [
        { key: 'name', header: 'Name', sortable: true },
        { key: 'projectKey', header: 'Key', sortable: true },
        { key: 'storyCount', header: 'Stories', sortable: true },
        { key: 'sprintCount', header: 'Sprints', sortable: true },
        {
            key: 'leadName',
            header: 'Lead',
            render: (row) => row.leadName ?? '—',
        },
        {
            key: 'status',
            header: 'Status',
            render: (row) => <Badge variant="status" value={row.status} />,
        },
    ];

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-semibold text-foreground">Projects</h1>
                {canCreate && (
                    <button
                        onClick={() => { setServerError(''); setModalOpen(true); }}
                        className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        <Plus size={16} />
                        Create Project
                    </button>
                )}
            </div>

            <DataTable
                columns={columns}
                data={projects}
                loading={loading}
                sortBy={sortBy}
                sortDirection={sortDirection}
                onSort={handleSort}
                onRowClick={(row) => navigate(`/projects/${row.projectId}`)}
                keyExtractor={(row) => row.projectId}
            />

            <Pagination
                page={page}
                pageSize={pageSize}
                totalCount={totalCount}
                onPageChange={setPage}
                onPageSizeChange={setPageSize}
            />

            <Modal open={modalOpen} onClose={() => setModalOpen(false)} title="Create Project">
                <ProjectForm
                    onSubmit={handleCreate}
                    submitting={submitting}
                    serverError={serverError}
                />
            </Modal>
        </div>
    );
}
