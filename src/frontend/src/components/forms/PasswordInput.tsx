import { useState, forwardRef, type InputHTMLAttributes } from 'react';
import { Eye, EyeOff } from 'lucide-react';

interface PasswordInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
    showStrength?: boolean;
}

function getStrength(password: string): { score: number; label: string; color: string } {
    let score = 0;
    if (password.length >= 8) score++;
    if (/[A-Z]/.test(password)) score++;
    if (/[a-z]/.test(password)) score++;
    if (/\d/.test(password)) score++;
    if (/[!@#$%^&*]/.test(password)) score++;

    if (score <= 2) return { score, label: 'Weak', color: 'bg-red-500' };
    if (score <= 3) return { score, label: 'Fair', color: 'bg-yellow-500' };
    if (score <= 4) return { score, label: 'Good', color: 'bg-blue-500' };
    return { score, label: 'Strong', color: 'bg-green-500' };
}

export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
    ({ showStrength = false, className = '', ...props }, ref) => {
        const [visible, setVisible] = useState(false);
        const value = typeof props.value === 'string' ? props.value : '';
        const strength = showStrength && value ? getStrength(value) : null;

        return (
            <div className="space-y-1.5">
                <div className="relative">
                    <input
                        ref={ref}
                        type={visible ? 'text' : 'password'}
                        className={`h-9 w-full rounded-md border border-input bg-background px-3 pr-9 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring ${className}`}
                        {...props}
                    />
                    <button
                        type="button"
                        onClick={() => setVisible((v) => !v)}
                        className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                        tabIndex={-1}
                        aria-label={visible ? 'Hide password' : 'Show password'}
                    >
                        {visible ? <EyeOff size={16} /> : <Eye size={16} />}
                    </button>
                </div>
                {strength && (
                    <div className="space-y-1">
                        <div className="flex gap-1">
                            {Array.from({ length: 5 }).map((_, i) => (
                                <div
                                    key={i}
                                    className={`h-1 flex-1 rounded-full ${i < strength.score ? strength.color : 'bg-muted'
                                        }`}
                                />
                            ))}
                        </div>
                        <p className="text-xs text-muted-foreground">{strength.label}</p>
                    </div>
                )}
            </div>
        );
    }
);

PasswordInput.displayName = 'PasswordInput';
