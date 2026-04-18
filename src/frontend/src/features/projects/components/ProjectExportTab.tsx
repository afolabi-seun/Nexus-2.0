import { useState } from 'react';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { Download } from 'lucide-react';

interface Props {
    projectId: string;
    projectName: string;
}

function downloadBlob(blob: Blob, filename: string) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
}

export function ProjectExportTab({ projectId, projectName }: Props) {
    const { addToast } = useToast();
    const [exporting, setExporting] = useState<string | null>(null);

    const handleExport = async (type: 'stories' | 'time-entries') => {
        setExporting(type);
        try {
            const blob = type === 'stories'
                ? await workApi.exportStoriesCsv({ projectId })
                : await workApi.exportTimeEntriesCsv({ projectId });
            const safeName = projectName.replace(/[^a-zA-Z0-9]/g, '_').toLowerCase();
            downloadBlob(blob, `${safeName}_${type}_${new Date().toISOString().slice(0, 10)}.csv`);
            addToast('success', `${type === 'stories' ? 'Stories' : 'Time entries'} exported`);
        } catch {
            addToast('error', `Failed to export ${type}`);
        } finally {
            setExporting(null);
        }
    };

    return (
        <div className="space-y-4">
            <h3 className="text-sm font-semibold text-foreground">Export Project Data</h3>
            <div className="grid gap-4 sm:grid-cols-2">
                <ExportCard
                    title="Stories"
                    description="Export all stories in this project as CSV. Includes status, priority, assignee, story points, and dates."
                    loading={exporting === 'stories'}
                    onExport={() => handleExport('stories')}
                />
                <ExportCard
                    title="Time Entries"
                    description="Export all time entries logged against stories in this project. Includes duration, date, billable status, and approval status."
                    loading={exporting === 'time-entries'}
                    onExport={() => handleExport('time-entries')}
                />
            </div>
        </div>
    );
}

function ExportCard({ title, description, loading, onExport }: {
    title: string;
    description: string;
    loading: boolean;
    onExport: () => void;
}) {
    return (
        <div className="rounded-lg border border-border bg-card p-4 space-y-3">
            <div>
                <p className="text-sm font-medium text-foreground">{title}</p>
                <p className="mt-1 text-xs text-muted-foreground">{description}</p>
            </div>
            <button
                onClick={onExport}
                disabled={loading}
                className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
                <Download size={14} />
                {loading ? 'Exporting...' : `Export ${title} CSV`}
            </button>
        </div>
    );
}
