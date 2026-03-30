import { useState } from 'react';

interface MarkdownEditorProps {
    value: string;
    onChange: (value: string) => void;
    placeholder?: string;
    rows?: number;
    id?: string;
}

export function MarkdownEditor({
    value,
    onChange,
    placeholder = 'Write markdown...',
    rows = 6,
    id,
}: MarkdownEditorProps) {
    const [preview, setPreview] = useState(false);

    return (
        <div className="rounded-md border border-input">
            <div className="flex border-b border-input">
                <button
                    type="button"
                    onClick={() => setPreview(false)}
                    className={`px-3 py-1.5 text-xs font-medium ${!preview
                            ? 'border-b-2 border-primary text-primary'
                            : 'text-muted-foreground hover:text-foreground'
                        }`}
                >
                    Write
                </button>
                <button
                    type="button"
                    onClick={() => setPreview(true)}
                    className={`px-3 py-1.5 text-xs font-medium ${preview
                            ? 'border-b-2 border-primary text-primary'
                            : 'text-muted-foreground hover:text-foreground'
                        }`}
                >
                    Preview
                </button>
            </div>

            {preview ? (
                <div className="min-h-[8rem] p-3 text-sm text-foreground whitespace-pre-wrap">
                    {value || <span className="text-muted-foreground">Nothing to preview</span>}
                </div>
            ) : (
                <textarea
                    id={id}
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    placeholder={placeholder}
                    rows={rows}
                    className="w-full resize-y bg-background p-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none"
                />
            )}
        </div>
    );
}
