import type { ReactNode } from 'react';

interface FormFieldProps {
    name: string;
    label: string;
    error?: string;
    required?: boolean;
    children: ReactNode;
}

export function FormField({ name, label, error, required, children }: FormFieldProps) {
    return (
        <div className="space-y-1.5">
            <label htmlFor={name} className="block text-sm font-medium text-foreground">
                {label}
                {required && <span className="ml-0.5 text-destructive">*</span>}
            </label>
            {children}
            {error && (
                <p className="text-xs text-destructive" role="alert">
                    {error}
                </p>
            )}
        </div>
    );
}
