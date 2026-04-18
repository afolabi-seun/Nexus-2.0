import { SprintProgressWidget } from '../components/SprintProgressWidget.js';
import { MyTasksWidget } from '../components/MyTasksWidget.js';
import { RecentActivityWidget } from '../components/RecentActivityWidget.js';
import { VelocityChartWidget } from '../components/VelocityChartWidget.js';
import { ProjectHealthWidget } from '../components/ProjectHealthWidget';

interface WidgetCardProps {
    title: string;
    children: React.ReactNode;
}

function WidgetCard({ title, children }: WidgetCardProps) {
    return (
        <div className="rounded-lg border border-border bg-card p-5 shadow-sm">
            <h2 className="mb-4 text-sm font-semibold text-foreground">{title}</h2>
            {children}
        </div>
    );
}

export function DashboardPage() {
    return (
        <div className="space-y-6">
            <h1 className="text-2xl font-bold text-foreground">Dashboard</h1>

            <div className="grid gap-6 md:grid-cols-2">
                <WidgetCard title="Sprint Progress">
                    <SprintProgressWidget />
                </WidgetCard>

                <WidgetCard title="My Tasks">
                    <MyTasksWidget />
                </WidgetCard>

                <WidgetCard title="Project Health">
                    <ProjectHealthWidget />
                </WidgetCard>

                <WidgetCard title="Velocity">
                    <VelocityChartWidget />
                </WidgetCard>

                <div className="md:col-span-2">
                    <WidgetCard title="Recent Activity">
                        <RecentActivityWidget />
                    </WidgetCard>
                </div>
            </div>
        </div>
    );
}
