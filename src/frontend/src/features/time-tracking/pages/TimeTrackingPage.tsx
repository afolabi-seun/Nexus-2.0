import { TimerWidget } from '../components/TimerWidget';
import { TimeEntryList } from '../components/TimeEntryList';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { Download } from 'lucide-react';

export function TimeTrackingPage() {
    const { addToast } = useToast();

    const handleExport = async () => {
        try {
            const blob = await workApi.exportTimeEntriesCsv({});
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `time_entries_${new Date().toISOString().slice(0, 10)}.csv`;
            a.click();
            URL.revokeObjectURL(url);
            addToast('success', 'Time entries exported');
        } catch {
            addToast('error', 'Failed to export time entries');
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-semibold text-foreground">Time Tracking</h1>
                <button
                    onClick={handleExport}
                    className="inline-flex items-center gap-1.5 rounded-md border border-input px-3 py-2 text-sm font-medium text-foreground hover:bg-accent"
                >
                    <Download size={14} /> Export CSV
                </button>
            </div>
            <TimerWidget />
            <TimeEntryList />
        </div>
    );
}
