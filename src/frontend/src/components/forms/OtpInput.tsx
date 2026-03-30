import { useRef, useState, useEffect, type KeyboardEvent, type ClipboardEvent } from 'react';

interface OtpInputProps {
    length?: number;
    onComplete: (otp: string) => void;
    disabled?: boolean;
}

export function OtpInput({ length = 6, onComplete, disabled = false }: OtpInputProps) {
    const [values, setValues] = useState<string[]>(Array(length).fill(''));
    const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

    useEffect(() => {
        inputRefs.current[0]?.focus();
    }, []);

    const handleChange = (index: number, val: string) => {
        if (!/^\d?$/.test(val)) return;
        const next = [...values];
        next[index] = val;
        setValues(next);

        if (val && index < length - 1) {
            inputRefs.current[index + 1]?.focus();
        }

        if (next.every((v) => v !== '')) {
            onComplete(next.join(''));
        }
    };

    const handleKeyDown = (index: number, e: KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Backspace' && !values[index] && index > 0) {
            inputRefs.current[index - 1]?.focus();
        }
    };

    const handlePaste = (e: ClipboardEvent<HTMLInputElement>) => {
        e.preventDefault();
        const pasted = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, length);
        if (!pasted) return;
        const next = [...values];
        for (let i = 0; i < pasted.length; i++) {
            next[i] = pasted[i];
        }
        setValues(next);
        const focusIdx = Math.min(pasted.length, length - 1);
        inputRefs.current[focusIdx]?.focus();
        if (next.every((v) => v !== '')) {
            onComplete(next.join(''));
        }
    };

    return (
        <div className="flex gap-2 justify-center">
            {values.map((val, i) => (
                <input
                    key={i}
                    ref={(el) => { inputRefs.current[i] = el; }}
                    type="text"
                    inputMode="numeric"
                    maxLength={1}
                    value={val}
                    disabled={disabled}
                    onChange={(e) => handleChange(i, e.target.value)}
                    onKeyDown={(e) => handleKeyDown(i, e)}
                    onPaste={i === 0 ? handlePaste : undefined}
                    className="h-12 w-10 rounded-md border border-input bg-background text-center text-lg font-medium text-foreground focus:outline-none focus:ring-2 focus:ring-ring disabled:opacity-50"
                    aria-label={`Digit ${i + 1}`}
                />
            ))}
        </div>
    );
}
