import { formatBytes } from '../utils/formatBytes';

interface UsageMeterProps {
    metricName: string;
    currentValue: number;
    limit: number;
    percentUsed: number;
}

const metricDisplayNames: Record<string, string> = {
    active_members: 'Active Members',
    stories_created: 'Stories Created',
    storage_bytes: 'Storage',
};

function getBarColor(percentUsed: number): string {
    if (percentUsed > 95) return 'bg-red-500';
    if (percentUsed > 80) return 'bg-amber-500';
    return 'bg-blue-500';
}

function formatValue(metricName: string, value: number): string {
    return metricName === 'storage_bytes' ? formatBytes(value) : String(value);
}

export function UsageMeter({ metricName, currentValue, limit, percentUsed }: UsageMeterProps) {
    const displayName = metricDisplayNames[metricName] ?? metricName;
    const isUnlimited = limit === 0;
    const currentFormatted = formatValue(metricName, currentValue);
    const limitFormatted = isUnlimited ? 'Unlimited' : formatValue(metricName, limit);
    const barWidth = isUnlimited ? 0 : Math.min(percentUsed, 100);

    return (
        <div className="space-y-2">
            <div className="flex items-center justify-between text-sm">
                <span className="font-medium text-foreground">{displayName}</span>
                <span className="text-muted-foreground">
                    {currentFormatted} / {limitFormatted}
                </span>
            </div>
            <div className="h-2 w-full rounded-full bg-muted">
                {!isUnlimited && (
                    <div
                        className={`h-2 rounded-full transition-all ${getBarColor(percentUsed)}`}
                        style={{ width: `${barWidth}%` }}
                    />
                )}
            </div>
            <div className="text-right text-xs text-muted-foreground">
                {isUnlimited ? 'Unlimited' : `${Math.round(percentUsed)}%`}
            </div>
        </div>
    );
}
