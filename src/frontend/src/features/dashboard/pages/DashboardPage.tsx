import { SprintProgressWidget } from '../components/SprintProgressWidget.js';
import { MyTasksWidget } from '../components/MyTasksWidget.js';
import { RecentActivityWidget } from '../components/RecentActivityWidget.js';
import { VelocityChartWidget } from '../components/VelocityChartWidget.js';
import { ProjectHealthWidget } from '../components/ProjectHealthWidget';
import { PendingApprovalsWidget } from '../components/PendingApprovalsWidget';
import { BillingUsageWidget } from '../components/BillingUsageWidget';
import { MyTimeWidget } from '../components/MyTimeWidget';
import { MyStoriesWidget } from '../components/MyStoriesWidget';
import { UpcomingDueDatesWidget } from '../components/UpcomingDueDatesWidget';
import { PageHeader } from '@/components/common/PageHeader';
import { useAuth } from '@/hooks/useAuth';

interface WidgetCardProps {
    title: string;
    children: React.ReactNode;
    span?: boolean;
}

function WidgetCard({ title, children, span }: WidgetCardProps) {
    return (
        <div className={`rounded-lg border border-border bg-card p-5 shadow-sm ${span ? 'md:col-span-2' : ''}`}>
            <h2 className="mb-4 text-sm font-semibold text-foreground">{title}</h2>
            {children}
        </div>
    );
}

const descriptions: Record<string, string> = {
    OrgAdmin: 'Organization overview — project health, team activity, billing, and pending approvals.',
    DeptLead: 'Department overview — sprint progress, team tasks, and pending approvals.',
    Member: 'Your work — assigned tasks, time logged, and upcoming due dates.',
    Viewer: 'Organization overview — project health, sprint progress, and velocity trends.',
};

export function DashboardPage() {
    const { user } = useAuth();
    const role = user?.roleName ?? 'Viewer';
    const desc = descriptions[role] ?? descriptions.Viewer;

    return (
        <div className="space-y-6">
            <PageHeader title="Dashboard" description={desc} dismissKey="dashboard" />

            <div className="grid gap-6 md:grid-cols-2">
                {/* Sprint Progress — all roles */}
                <WidgetCard title="Sprint Progress">
                    <SprintProgressWidget />
                </WidgetCard>

                {/* My Tasks — OrgAdmin, DeptLead, Member (not Viewer) */}
                {role !== 'Viewer' && (
                    <WidgetCard title="My Tasks">
                        <MyTasksWidget />
                    </WidgetCard>
                )}

                {/* Project Health — OrgAdmin, DeptLead, Viewer */}
                {(role === 'OrgAdmin' || role === 'DeptLead' || role === 'Viewer') && (
                    <WidgetCard title="Project Health">
                        <ProjectHealthWidget />
                    </WidgetCard>
                )}

                {/* My Time This Week — Member only */}
                {role === 'Member' && (
                    <WidgetCard title="My Time This Week">
                        <MyTimeWidget />
                    </WidgetCard>
                )}

                {/* My Stories — Member only */}
                {role === 'Member' && (
                    <WidgetCard title="My Stories">
                        <MyStoriesWidget />
                    </WidgetCard>
                )}

                {/* Upcoming Due Dates — OrgAdmin, DeptLead, Member (anyone with assigned work) */}
                {role !== 'Viewer' && (
                    <WidgetCard title="Upcoming Due Dates">
                        <UpcomingDueDatesWidget />
                    </WidgetCard>
                )}

                {/* Velocity — OrgAdmin, DeptLead, Viewer */}
                {(role === 'OrgAdmin' || role === 'DeptLead' || role === 'Viewer') && (
                    <WidgetCard title="Velocity">
                        <VelocityChartWidget />
                    </WidgetCard>
                )}

                {/* Pending Approvals — OrgAdmin, DeptLead */}
                {(role === 'OrgAdmin' || role === 'DeptLead') && (
                    <WidgetCard title="Pending Approvals">
                        <PendingApprovalsWidget />
                    </WidgetCard>
                )}

                {/* Billing Usage — OrgAdmin only */}
                {role === 'OrgAdmin' && (
                    <WidgetCard title="Plan Usage">
                        <BillingUsageWidget />
                    </WidgetCard>
                )}

                {/* Recent Activity — all roles, full width */}
                <WidgetCard title="Recent Activity" span>
                    <RecentActivityWidget />
                </WidgetCard>
            </div>
        </div>
    );
}
