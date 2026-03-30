const PRESET_COLORS = [
    '#ef4444', '#f97316', '#eab308', '#22c55e', '#06b6d4',
    '#3b82f6', '#8b5cf6', '#ec4899', '#6b7280', '#1e293b',
];

interface ColorPickerProps {
    value: string;
    onChange: (color: string) => void;
}

export function ColorPicker({ value, onChange }: ColorPickerProps) {
    return (
        <div className="space-y-2">
            <div className="flex flex-wrap gap-2">
                {PRESET_COLORS.map((color) => (
                    <button
                        key={color}
                        type="button"
                        onClick={() => onChange(color)}
                        className={`h-7 w-7 rounded-full border-2 transition-transform ${value === color ? 'border-foreground scale-110' : 'border-transparent'
                            }`}
                        style={{ backgroundColor: color }}
                        aria-label={`Select color ${color}`}
                    />
                ))}
            </div>
            <div className="flex items-center gap-2">
                <input
                    type="color"
                    value={value || '#3b82f6'}
                    onChange={(e) => onChange(e.target.value)}
                    className="h-8 w-8 cursor-pointer rounded border border-input"
                    aria-label="Custom color"
                />
                <input
                    type="text"
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    placeholder="#000000"
                    className="h-8 w-24 rounded-md border border-input bg-background px-2 text-sm text-foreground"
                />
            </div>
        </div>
    );
}
