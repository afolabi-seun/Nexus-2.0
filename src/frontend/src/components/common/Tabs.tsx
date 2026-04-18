import { useState } from 'react';

export interface TabItem {
    key: string;
    label: string;
    icon?: React.ReactNode;
}

interface TabsProps {
    tabs: TabItem[];
    activeTab: string;
    onTabChange: (key: string) => void;
}

export function Tabs({ tabs, activeTab, onTabChange }: TabsProps) {
    return (
        <div className="flex gap-1 border-b border-border" role="tablist">
            {tabs.map((tab) => (
                <button
                    key={tab.key}
                    role="tab"
                    aria-selected={activeTab === tab.key}
                    onClick={() => onTabChange(tab.key)}
                    className={`flex items-center gap-1.5 border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
                        activeTab === tab.key
                            ? 'border-primary text-primary'
                            : 'border-transparent text-muted-foreground hover:border-border hover:text-foreground'
                    }`}
                >
                    {tab.icon}
                    {tab.label}
                </button>
            ))}
        </div>
    );
}

export function useTab(defaultTab: string) {
    const [activeTab, setActiveTab] = useState(defaultTab);
    return { activeTab, setActiveTab };
}
