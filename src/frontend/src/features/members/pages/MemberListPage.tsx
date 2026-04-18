import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { profileApi } from '@/api/profileApi';
import { DataTable, type Column } from '@/components/common/DataTable';
import { Badge } from '@/components/common/Badge';
import { Pagination } from '@/components/common/Pagination';
import { useToast } from '@/components/common/Toast';
import { usePagination } from '@/hooks/usePagination';
import { useOrg } from '@/hooks/useOrg';
import { ListFilter } from '@/components/common/ListFilter';
import { useListFilters } from '@/hooks/useListFilters';
import type { FilterConfig } from '@/types/filters';
import type { TeamMember } from '@/types/profile';
import { PageHeader } from '@/components/common/PageHeader';

export function MemberListPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const { departments } = useOrg();
    const { page, pageSize, setPage, setPageSize } = usePagination();

    const [members, setMembers] = useState<TeamMember[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [loading, setLoading] = useState(true);

    const filterConfigs: FilterConfig[] = [
        {
            key: 'departmentId',
            label: 'Department',
            type: 'select',
            options: departments.map((d) => ({ value: d.departmentId, label: d.name })),
        },
        {
            key: 'roleName',
            label: 'Role',
            type: 'select',
            options: [
                { value: 'OrgAdmin', label: 'OrgAdmin' },
                { value: 'DeptLead', label: 'DeptLead' },
                { value: 'Member', label: 'Member' },
                { value: 'Viewer', label: 'Viewer' },
            ],
        },
        {
            key: 'status',
            label: 'Status',
            type: 'select',
            options: [
                { value: 'A', label: 'Active' },
                { value: 'S', label: 'Suspended' },
                { value: 'D', label: 'Deactivated' },
            ],
        },
        {
            key: 'availability',
            label: 'Availability',
            type: 'select',
            options: [
                { value: 'Available', label: 'Available' },
                { value: 'Busy', label: 'Busy' },
                { value: 'Away', label: 'Away' },
                { value: 'Offline', label: 'Offline' },
            ],
        },
    ];

    const { filterValues, updateFilter, clearFilters, hasActiveFilters, activeFilterCount } =
        useListFilters(filterConfigs, { onPageReset: () => setPage(1) });

    const fetchMembers = useCallback(async () => {
        setLoading(true);
        try {
            const res = await profileApi.getTeamMembers({
                page,
                pageSize,
                departmentId: filterValues.departmentId as string | undefined,
                roleName: filterValues.roleName as string | undefined,
                status: filterValues.status as string | undefined,
                availability: filterValues.availability as string | undefined,
            });
            setMembers(res.data);
            setTotalCount(res.totalCount);
        } catch {
            addToast('error', 'Failed to load members');
        } finally {
            setLoading(false);
        }
    }, [page, pageSize, filterValues, addToast]);

    useEffect(() => { fetchMembers(); }, [fetchMembers]);

    const columns: Column<TeamMember>[] = [
        { key: 'name', header: 'Name', render: (m) => `${m.firstName} ${m.lastName}` },
        { key: 'professionalId', header: 'Professional ID' },
        { key: 'email', header: 'Email' },
        { key: 'departmentName', header: 'Department', render: (m) => m.departmentName ?? '—' },
        { key: 'roleName', header: 'Role', render: (m) => m.roleName ? <Badge variant="role" value={m.roleName} /> : <>—</> },
        { key: 'availability', header: 'Availability', render: (m) => <Badge variant="status" value={m.availability} /> },
        { key: 'flgStatus', header: 'Status', render: (m) => <Badge variant="status" value={m.flgStatus === 'A' ? 'Active' : m.flgStatus === 'S' ? 'Suspended' : 'Deactivated'} /> },
    ];

    return (
        <div className="space-y-4">
            <PageHeader title="Members" description="Your organization's team members. Manage roles, departments, and availability." dismissKey="members" />

            <ListFilter
                configs={filterConfigs}
                values={filterValues}
                onUpdateFilter={updateFilter}
                onClearFilters={clearFilters}
                hasActiveFilters={hasActiveFilters}
                activeFilterCount={activeFilterCount}
            />

            <DataTable
                columns={columns}
                data={members}
                loading={loading}
                keyExtractor={(m) => m.teamMemberId}
                onRowClick={(m) => navigate(`/members/${m.teamMemberId}`)}
            />
            <Pagination
                page={page}
                pageSize={pageSize}
                totalCount={totalCount}
                onPageChange={setPage}
                onPageSizeChange={setPageSize}
            />
        </div>
    );
}
