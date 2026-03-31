import { useState } from 'react';
import { CostRateManager } from '../components/CostRateManager';
import { TimePolicySettings } from '../components/TimePolicySettings';

const TABS = ['Cost Rates', 'Time Policy'] as const;
type Tab = (typeof TABS)[number];

export function TimeTrackingSettingsPage() {
    const [activeTab, setActiveTab] = useState<Tab>('Cost Rates');

    return (
        <div className="space-y-6">
            <h1 className="text-2xl font-semibold text-foreground">Time Tracking Settings</h1>

            <div className="flex gap-1 border-b border-border">
                {TABS.map((tab) => (
                    <button
                        key={tab}
                        onClick={() => setActiveTab(tab)}
                        className={`px-4 py-2 text-sm font-medium transition-colors ${activeTab === tab
                                ? 'border-b-2 border-primary text-primary'
                                : 'text-muted-foreground hover:text-foreground'
                            }`}
                    >
                        {tab}
                    </button>
                ))}
            </div>

            {activeTab === 'Cost Rates' && <CostRateManager />}
            {activeTab === 'Time Policy' && <TimePolicySettings />}
        </div>
    );
}
