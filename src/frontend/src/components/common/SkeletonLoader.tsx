interface SkeletonLoaderProps {
    variant?: 'table' | 'card' | 'form' | 'board';
    rows?: number;
    columns?: number;
}

function SkeletonBar({ className = '' }: { className?: string }) {
    return <div className={`animate-pulse rounded bg-muted ${className}`} />;
}

function TableSkeleton({ rows = 5, columns = 5 }: { rows?: number; columns?: number }) {
    return (
        <div className="space-y-3">
            <div className="flex gap-4">
                {Array.from({ length: columns }).map((_, i) => (
                    <SkeletonBar key={i} className="h-4 flex-1" />
                ))}
            </div>
            {Array.from({ length: rows }).map((_, r) => (
                <div key={r} className="flex gap-4">
                    {Array.from({ length: columns }).map((_, c) => (
                        <SkeletonBar key={c} className="h-3 flex-1" />
                    ))}
                </div>
            ))}
        </div>
    );
}

function CardSkeleton() {
    return (
        <div className="space-y-3 rounded-lg border border-border p-4">
            <SkeletonBar className="h-5 w-3/4" />
            <SkeletonBar className="h-3 w-full" />
            <SkeletonBar className="h-3 w-1/2" />
        </div>
    );
}

function FormSkeleton() {
    return (
        <div className="space-y-4">
            {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="space-y-2">
                    <SkeletonBar className="h-3 w-24" />
                    <SkeletonBar className="h-9 w-full" />
                </div>
            ))}
        </div>
    );
}

function BoardSkeleton({ columns = 4 }: { columns?: number }) {
    return (
        <div className="flex gap-4">
            {Array.from({ length: columns }).map((_, i) => (
                <div key={i} className="flex-1 space-y-3">
                    <SkeletonBar className="h-5 w-24" />
                    <CardSkeleton />
                    <CardSkeleton />
                </div>
            ))}
        </div>
    );
}

export function SkeletonLoader({ variant = 'table', rows, columns }: SkeletonLoaderProps) {
    switch (variant) {
        case 'table':
            return <TableSkeleton rows={rows} columns={columns} />;
        case 'card':
            return <CardSkeleton />;
        case 'form':
            return <FormSkeleton />;
        case 'board':
            return <BoardSkeleton columns={columns} />;
    }
}
