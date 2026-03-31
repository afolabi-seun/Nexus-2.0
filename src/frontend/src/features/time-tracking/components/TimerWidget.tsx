import { useState, useEffect, useRef } from 'react';
import { Play, Square } from 'lucide-react';
import { useTimeTrackingStore } from '@/stores/timeTrackingStore';
import { useToast } from '@/components/common/Toast';

function formatElapsed(totalSeconds: number): string {
    const h = Math.floor(totalSeconds / 3600);
    const m = Math.floor((totalSeconds % 3600) / 60);
    const s = totalSeconds % 60;
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
}

export function TimerWidget() {
    const { addToast } = useToast();
    const { timerStatus, fetchTimerStatus, startTimer, stopTimer } = useTimeTrackingStore();
    const [storyInput, setStoryInput] = useState('');
    const [elapsed, setElapsed] = useState(0);
    const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

    useEffect(() => {
        fetchTimerStatus();
    }, [fetchTimerStatus]);

    useEffect(() => {
        if (timerStatus?.isRunning) {
            setElapsed(timerStatus.elapsedSeconds);
            intervalRef.current = setInterval(() => {
                setElapsed((prev) => prev + 1);
            }, 1000);
        } else {
            setElapsed(0);
            if (intervalRef.current) clearInterval(intervalRef.current);
        }
        return () => {
            if (intervalRef.current) clearInterval(intervalRef.current);
        };
    }, [timerStatus]);

    const handleStart = async () => {
        if (!storyInput.trim()) {
            addToast('error', 'Enter a story ID');
            return;
        }
        try {
            await startTimer({ storyId: storyInput.trim() });
            setStoryInput('');
            addToast('success', 'Timer started');
        } catch {
            addToast('error', 'Failed to start timer');
        }
    };

    const handleStop = async () => {
        try {
            await stopTimer();
            addToast('success', 'Timer stopped — time entry created');
            // Refresh entries if parent needs it
        } catch {
            addToast('error', 'Failed to stop timer');
        }
    };

    const isRunning = timerStatus?.isRunning ?? false;

    return (
        <div className="flex items-center gap-3 rounded-lg border border-border bg-card px-4 py-3">
            {isRunning ? (
                <>
                    <div className="flex items-center gap-2">
                        <span className="h-2 w-2 animate-pulse rounded-full bg-green-500" />
                        <span className="text-sm font-medium text-foreground">
                            {timerStatus?.storyKey ?? 'Timer'}
                        </span>
                        {timerStatus?.storyTitle && (
                            <span className="text-sm text-muted-foreground truncate max-w-[200px]">
                                — {timerStatus.storyTitle}
                            </span>
                        )}
                    </div>
                    <span className="font-mono text-lg font-semibold text-foreground tabular-nums">
                        {formatElapsed(elapsed)}
                    </span>
                    <button
                        onClick={handleStop}
                        className="inline-flex items-center gap-1.5 rounded-md bg-destructive px-3 py-1.5 text-sm font-medium text-destructive-foreground hover:bg-destructive/90"
                    >
                        <Square size={14} /> Stop
                    </button>
                </>
            ) : (
                <>
                    <input
                        className="rounded-md border border-input bg-background px-2 py-1.5 text-sm"
                        placeholder="Story ID"
                        value={storyInput}
                        onChange={(e) => setStoryInput(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && handleStart()}
                    />
                    <button
                        onClick={handleStart}
                        className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        <Play size={14} /> Start Timer
                    </button>
                </>
            )}
        </div>
    );
}
