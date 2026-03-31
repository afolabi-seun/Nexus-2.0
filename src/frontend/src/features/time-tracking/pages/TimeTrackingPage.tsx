import { TimerWidget } from '../components/TimerWidget';
import { TimeEntryList } from '../components/TimeEntryList';

export function TimeTrackingPage() {
    return (
        <div className="space-y-6">
            <h1 className="text-2xl font-semibold text-foreground">Time Tracking</h1>
            <TimerWidget />
            <TimeEntryList />
        </div>
    );
}
