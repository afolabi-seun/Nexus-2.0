import { useOrgStore } from '@/stores/orgStore';

export function useOrg() {
    const organization = useOrgStore((s) => s.organization);
    const departments = useOrgStore((s) => s.departments);
    const referenceData = useOrgStore((s) => s.referenceData);
    const refresh = useOrgStore((s) => s.refresh);

    return { organization, departments, referenceData, refresh };
}
