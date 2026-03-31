import { useState, useEffect } from 'react';
import { useTimeTrackingStore } from '@/stores/timeTrackingStore';
import { useToast } from '@/components/common/Toast';

const WORKFLOWS = ['None', 'ManagerApproval', 'TwoStepApproval'] as const;

export function TimePolicySettings() {
    const { addToast } = useToast();
    const { timePolicy, loading, fetchTimePolicy, updateTimePolicy } = useTimeTrackingStore();

    const [requiredHoursPerDay, setRequiredHoursPerDay] = useState('8');
    const [overtimeThreshold, setOvertimeThreshold] = useState('40');
    const [approvalRequired, setApprovalRequired] = useState(true);
    const [approvalWorkflow, setApprovalWorkflow] = useState('ManagerApproval');
    const [maxDailyHours, setMaxDailyHours] = useState('24');

    useEffect(() => {
        fetchTimePolicy();
    }, [fetchTimePolicy]);

    useEffect(() => {
        if (timePolicy) {
            setRequiredHoursPerDay(String(timePolicy.requiredHoursPerDay));
            setOvertimeThreshold(String(timePolicy.overtimeThreshold));
            setApprovalRequired(timePolicy.approvalRequired);
            setApprovalWorkflow(timePolicy.approvalWorkflow);
            setMaxDailyHours(String(timePolicy.maxDailyHours));
        }
    }, [timePolicy]);

    const handleSave = async () => {
        try {
            await updateTimePolicy({
                requiredHoursPerDay: parseFloat(requiredHoursPerDay),
                overtimeThreshold: parseFloat(overtimeThreshold),
                approvalRequired,
                approvalWorkflow,
                maxDailyHours: parseFloat(maxDailyHours),
            });
            addToast('success', 'Time policy updated');
        } catch {
            addToast('error', 'Failed to update time policy');
        }
    };

    const inputCls = 'rounded-md border border-input bg-background px-3 py-1.5 text-sm w-full';

    return (
        <div className="space-y-4">
            <h2 className="text-lg font-semibold text-foreground">Time Policy</h2>

            {loading && <p className="text-sm text-muted-foreground">Loading…</p>}

            <div className="grid max-w-md gap-4">
                <label className="space-y-1">
                    <span className="text-sm font-medium text-foreground">Required Hours / Day</span>
                    <input className={inputCls} type="number" step="0.5" min="0" value={requiredHoursPerDay} onChange={(e) => setRequiredHoursPerDay(e.target.value)} />
                </label>
                <label className="space-y-1">
                    <span className="text-sm font-medium text-foreground">Overtime Threshold (weekly hours)</span>
                    <input className={inputCls} type="number" step="0.5" min="0" value={overtimeThreshold} onChange={(e) => setOvertimeThreshold(e.target.value)} />
                </label>
                <label className="flex items-center gap-2 text-sm font-medium text-foreground">
                    <input type="checkbox" checked={approvalRequired} onChange={(e) => setApprovalRequired(e.target.checked)} />
                    Approval Required
                </label>
                <label className="space-y-1">
                    <span className="text-sm font-medium text-foreground">Approval Workflow</span>
                    <select className={inputCls} value={approvalWorkflow} onChange={(e) => setApprovalWorkflow(e.target.value)}>
                        {WORKFLOWS.map((w) => <option key={w} value={w}>{w}</option>)}
                    </select>
                </label>
                <label className="space-y-1">
                    <span className="text-sm font-medium text-foreground">Max Daily Hours</span>
                    <input className={inputCls} type="number" step="0.5" min="0" value={maxDailyHours} onChange={(e) => setMaxDailyHours(e.target.value)} />
                </label>
            </div>

            <button onClick={handleSave} disabled={loading} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                Save Policy
            </button>
        </div>
    );
}
