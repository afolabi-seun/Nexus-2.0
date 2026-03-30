import { forwardRef, type InputHTMLAttributes } from 'react';

interface DatePickerProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'onChange'> {
    value?: string;
    onChange: (value: string) => void;
    minDate?: string;
    maxDate?: string;
}

export const DatePicker = forwardRef<HTMLInputElement, DatePickerProps>(
    ({ value, onChange, minDate, maxDate, className = '', ...props }, ref) => {
        return (
            <input
                ref={ref}
                type="date"
                value={value ?? ''}
                onChange={(e) => onChange(e.target.value)}
                min={minDate}
                max={maxDate}
                className={`h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring ${className}`}
                {...props}
            />
        );
    }
);

DatePicker.displayName = 'DatePicker';
