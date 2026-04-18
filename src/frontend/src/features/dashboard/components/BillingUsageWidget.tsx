import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { billingApi } from '@/api/billingApi';
import type { UsageMetric } from '@/types/billing';
import { ArrowRight } from 'lucide-react';

const metricLabels: Record<string, string> = {
    active_members: 'Team Members',
    stories_created: 'Stories This Month',
    storage_bytes: 'Storage',
};

function barColor(pct: number): string {
    if (pct >= 90) return 'bg-destructive';
    if (pct >= 70) return 'bg-yellow-500';
    return 'bg-primary';
}

export function BillingUsageWidget() {
    const navigate = useNavigate();
    const [metrics, setMetrics] = useState<UsageMetric[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        (async () => {
            try {
                const res = await billingApi.getUsage();
                setMetrics(res.metrics.filter((m) => m.limit > 0));
            } catch {
                // non-critical
            } finally {
                setLoading(false);
            }
        })();
    }, []);

    if (loading) return <p className="text-sm text-muted-foreground">Loading…</p>;
    if (metrics.length === 0) return <p className="text-sm text-muted-foreground">No usage limits on current plan.</p>;

    return (
        <div className="space-y-3">
            {metrics.map((m) => (
                <div key={m.metricName} className="space-y-1">
                    <div className="flex items-center justify-between text-xs">
                        <span className="text-muted-foreground">{metricLabels[m.metricName] ?? m.metricName}</span>
                        <span className="text-foreground font-medium">{m.currentValue} / {m.limit}</span>
                    </div>
                    <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                        <div
                            className={`h-full rounded-full transition-all ${barColor(m.percentUsed)}`}
                            style={{ width: `${Math.min(100, m.percentUsed)}%` }}
                        />
                    </div>
                </div>
            ))}
            <button
                onClick={() => navigate('/billing')}
                className="flex items-center gap-1 text-xs text-primary hover:underline"
            >
                Manage plan <ArrowRight size={12} />
            </button>
        </div>
    );
}
