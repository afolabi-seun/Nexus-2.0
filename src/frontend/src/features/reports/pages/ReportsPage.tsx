import { useState } from 'react';
import { useOrg } from '@/hooks/useOrg';
import type { ReportFilters } from '@/types/work';
import { VelocityChart } from '../components/VelocityChart.js';
import { DepartmentWorkloadChart } from '../components/DepartmentWorkloadChart.js';
import { CapacityUtilizationChart } from '../components/CapacityUtilizationChart.js';
import { CycleTimeChart } from '../components/CycleTimeChart.js';
import { TaskCompletionChart } from '../components/TaskCompletionChart.js';
import { BarChart3, Users, Gauge, Clock, CheckSquare } from 'lucide-react';

const TABS = [
    { key: 'velocity', label: 'Velocity', icon: <BarChart3 size={16} /> },
    { key: 'workload', label: 'Dept Workload', icon: <Users size={16} /> },
    { key: 'capacity', label: 'Capacity', icon: <Gauge size={16} /> },
    { key: 'cycleTime', label: 'Cycle Time', icon: <Clock size={16} /> },
    { key: 'completion', label: 'Completion', icon: <CheckSquare size={16} /> },
] as const;

type TabKey = (typeof TABS)[number]['key'];

export function ReportsPage() {
    const { departments } = useOrg();
    const [activeTab, setActiveTab] = useState<TabKey>('velocity');
    const [filters, setFilters] = useState<ReportFilters>({});

    return (
        <div className="space-y-4">
            <h1 className="text-2xl font-semibold text-foreground">Reports</h1>

            {/* Filters */}
            <div className="flex flex-wrap gap-3">
                <select
                    value={filters.projectId ?? ''}
                    onChange={(e) => setFilters((f) => ({ ...f, projectId: e.target.value || undefined }))}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                >
                    <option value="">All Projects</option>
                </select>
                <select
                    value={filters.departmentId ?? ''}
                    onChange={(e) => setFilters((f) => ({ ...f, departmentId: e.target.value || undefined }))}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                >
                    <option value="">All Departments</option>
                    {departments.map((d) => <option key={d.departmentId} value={d.departmentId}>{d.name}</option>)}
                </select>
                <input
                    type="date"
                    value={filters.dateFrom ?? ''}
                    onChange={(e) => setFilters((f) => ({ ...f, dateFrom: e.target.value || undefined }))}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                />
                <input
                    type="date"
                    value={filters.dateTo ?? ''}
                    onChange={(e) => setFilters((f) => ({ ...f, dateTo: e.target.value || undefined }))}
                    className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                />
            </div>

            {/* Tabs */}
            <div className="flex gap-1 border-b border-border">
                {TABS.map((tab) => (
                    <button
                        key={tab.key}
                        onClick={() => setActiveTab(tab.key)}
                        className={`inline-flex items-center gap-1.5 border-b-2 px-4 py-2 text-sm font-medium transition-colors ${activeTab === tab.key
                            ? 'border-primary text-primary'
                            : 'border-transparent text-muted-foreground hover:text-foreground'
                            }`}
                    >
                        {tab.icon} {tab.label}
                    </button>
                ))}
            </div>

            {/* Chart content */}
            <div className="rounded-md border border-border bg-card p-4">
                {activeTab === 'velocity' && <VelocityChart filters={filters} />}
                {activeTab === 'workload' && <DepartmentWorkloadChart filters={filters} />}
                {activeTab === 'capacity' && <CapacityUtilizationChart filters={filters} />}
                {activeTab === 'cycleTime' && <CycleTimeChart filters={filters} />}
                {activeTab === 'completion' && <TaskCompletionChart filters={filters} />}
            </div>
        </div>
    );
}
