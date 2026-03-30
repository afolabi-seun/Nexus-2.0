import { useState, useRef } from 'react';
import { Eye, EyeOff, Send } from 'lucide-react';
import { MentionAutocomplete } from './MentionAutocomplete.js';

interface CommentInputProps {
    onSubmit: (content: string) => Promise<void>;
    onCancel?: () => void;
    initialValue?: string;
    placeholder?: string;
    submitLabel?: string;
}

function renderPreview(text: string): string {
    return text
        .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
        .replace(/\*(.+?)\*/g, '<em>$1</em>')
        .replace(/`(.+?)`/g, '<code class="rounded bg-muted px-1 py-0.5 text-xs">$1</code>')
        .replace(/\[(.+?)\]\((.+?)\)/g, '<a href="$2" class="text-primary underline" target="_blank" rel="noopener noreferrer">$1</a>')
        .replace(/\n/g, '<br/>');
}

export function CommentInput({
    onSubmit,
    onCancel,
    initialValue = '',
    placeholder = 'Write a comment...',
    submitLabel = 'Comment',
}: CommentInputProps) {
    const [content, setContent] = useState(initialValue);
    const [preview, setPreview] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [showMention, setShowMention] = useState(false);
    const [mentionQuery, setMentionQuery] = useState('');
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    const handleChange = (value: string) => {
        setContent(value);
        // Check for @ mention trigger
        const textarea = textareaRef.current;
        if (textarea) {
            const cursorPos = textarea.selectionStart;
            const textBefore = value.slice(0, cursorPos);
            const atMatch = textBefore.match(/@(\w*)$/);
            if (atMatch) {
                setShowMention(true);
                setMentionQuery(atMatch[1]);
            } else {
                setShowMention(false);
                setMentionQuery('');
            }
        }
    };

    const handleMentionSelect = (name: string) => {
        const textarea = textareaRef.current;
        if (!textarea) return;
        const cursorPos = textarea.selectionStart;
        const textBefore = content.slice(0, cursorPos);
        const textAfter = content.slice(cursorPos);
        const atIdx = textBefore.lastIndexOf('@');
        const newContent = textBefore.slice(0, atIdx) + `@${name} ` + textAfter;
        setContent(newContent);
        setShowMention(false);
        setMentionQuery('');
        textarea.focus();
    };

    const handleSubmit = async () => {
        if (!content.trim()) return;
        setSubmitting(true);
        try {
            await onSubmit(content.trim());
            setContent('');
            setPreview(false);
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div className="space-y-2">
            <div className="relative">
                {preview ? (
                    <div
                        className="min-h-[80px] rounded-md border border-border bg-background p-3 text-sm text-foreground"
                        dangerouslySetInnerHTML={{ __html: renderPreview(content) }}
                    />
                ) : (
                    <textarea
                        ref={textareaRef}
                        value={content}
                        onChange={(e) => handleChange(e.target.value)}
                        placeholder={placeholder}
                        rows={3}
                        className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring resize-none"
                    />
                )}
                {showMention && !preview && (
                    <MentionAutocomplete query={mentionQuery} onSelect={handleMentionSelect} />
                )}
            </div>
            <div className="flex items-center justify-between">
                <button
                    type="button"
                    onClick={() => setPreview(!preview)}
                    className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
                >
                    {preview ? <EyeOff size={12} /> : <Eye size={12} />}
                    {preview ? 'Edit' : 'Preview'}
                </button>
                <div className="flex gap-2">
                    {onCancel && (
                        <button
                            type="button"
                            onClick={onCancel}
                            className="rounded-md border border-input px-3 py-1.5 text-xs font-medium text-foreground hover:bg-accent"
                        >
                            Cancel
                        </button>
                    )}
                    <button
                        type="button"
                        onClick={handleSubmit}
                        disabled={!content.trim() || submitting}
                        className="inline-flex items-center gap-1 rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                    >
                        <Send size={12} /> {submitting ? 'Posting...' : submitLabel}
                    </button>
                </div>
            </div>
        </div>
    );
}
