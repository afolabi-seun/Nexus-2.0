import { useState } from 'react';
import { Pencil, Trash2, Reply } from 'lucide-react';
import type { Comment } from '@/types/work';
import { CommentInput } from './CommentInput.js';

interface CommentItemProps {
    comment: Comment;
    isAuthor: boolean;
    canDelete: boolean;
    onEdit: (commentId: string, content: string) => Promise<void>;
    onDelete: (commentId: string) => void;
    onReply: (parentCommentId: string, content: string) => Promise<void>;
    isAuthorFn: (authorId: string) => boolean;
    canDeleteFn: (authorId: string) => boolean;
}

function renderMarkdown(text: string): string {
    return text
        .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
        .replace(/\*(.+?)\*/g, '<em>$1</em>')
        .replace(/`(.+?)`/g, '<code class="rounded bg-muted px-1 py-0.5 text-xs">$1</code>')
        .replace(/\[(.+?)\]\((.+?)\)/g, '<a href="$2" class="text-primary underline" target="_blank" rel="noopener noreferrer">$1</a>')
        .replace(/\n/g, '<br/>');
}

export function CommentItem({
    comment,
    isAuthor,
    canDelete,
    onEdit,
    onDelete,
    onReply,
    isAuthorFn,
    canDeleteFn,
}: CommentItemProps) {
    const [editing, setEditing] = useState(false);
    const [replying, setReplying] = useState(false);

    const initials = comment.authorName
        ? comment.authorName.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase()
        : '??';

    return (
        <div className="space-y-2">
            <div className="flex gap-3 rounded-md border border-border p-3">
                <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary text-xs font-medium text-primary-foreground">
                    {initials}
                </div>
                <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                        <span className="text-sm font-medium text-foreground">{comment.authorName}</span>
                        <span className="text-xs text-muted-foreground">
                            {new Date(comment.dateCreated).toLocaleString()}
                        </span>
                        {comment.isEdited && (
                            <span className="text-xs text-muted-foreground">(edited)</span>
                        )}
                    </div>

                    {editing ? (
                        <CommentInput
                            initialValue={comment.content}
                            onSubmit={async (content) => {
                                await onEdit(comment.commentId, content);
                                setEditing(false);
                            }}
                            onCancel={() => setEditing(false)}
                            submitLabel="Save"
                        />
                    ) : (
                        <div
                            className="mt-1 text-sm text-foreground prose-sm"
                            dangerouslySetInnerHTML={{ __html: renderMarkdown(comment.content) }}
                        />
                    )}

                    {!editing && (
                        <div className="mt-2 flex gap-2">
                            <button
                                onClick={() => setReplying(!replying)}
                                className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
                            >
                                <Reply size={12} /> Reply
                            </button>
                            {isAuthor && (
                                <button
                                    onClick={() => setEditing(true)}
                                    className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
                                >
                                    <Pencil size={12} /> Edit
                                </button>
                            )}
                            {canDelete && (
                                <button
                                    onClick={() => onDelete(comment.commentId)}
                                    className="inline-flex items-center gap-1 text-xs text-destructive hover:text-destructive/80"
                                >
                                    <Trash2 size={12} /> Delete
                                </button>
                            )}
                        </div>
                    )}
                </div>
            </div>

            {replying && (
                <div className="ml-11">
                    <CommentInput
                        onSubmit={async (content) => {
                            await onReply(comment.commentId, content);
                            setReplying(false);
                        }}
                        onCancel={() => setReplying(false)}
                        placeholder="Write a reply..."
                        submitLabel="Reply"
                    />
                </div>
            )}

            {/* Nested replies */}
            {comment.replies && comment.replies.length > 0 && (
                <div className="ml-8 space-y-2 border-l-2 border-border pl-3">
                    {comment.replies.map((reply) => (
                        <CommentItem
                            key={reply.commentId}
                            comment={reply}
                            isAuthor={isAuthorFn(reply.authorId)}
                            canDelete={canDeleteFn(reply.authorId)}
                            onEdit={onEdit}
                            onDelete={onDelete}
                            onReply={onReply}
                            isAuthorFn={isAuthorFn}
                            canDeleteFn={canDeleteFn}
                        />
                    ))}
                </div>
            )}
        </div>
    );
}
