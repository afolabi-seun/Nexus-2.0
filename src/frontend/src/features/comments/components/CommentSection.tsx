import { useState, useEffect, useCallback } from 'react';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { useAuth } from '@/hooks/useAuth';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { Comment } from '@/types/work';
import { CommentItem } from './CommentItem.js';
import { CommentInput } from './CommentInput.js';

interface CommentSectionProps {
    entityType: 'Story' | 'Task';
    entityId: string;
}

export function CommentSection({ entityType, entityId }: CommentSectionProps) {
    const { addToast } = useToast();
    const { user } = useAuth();
    const [comments, setComments] = useState<Comment[]>([]);
    const [loading, setLoading] = useState(true);
    const [deleteTarget, setDeleteTarget] = useState<string | null>(null);

    const fetchComments = useCallback(async () => {
        try {
            const data = await workApi.getComments(entityType, entityId);
            setComments(data);
        } catch {
            addToast('error', 'Failed to load comments');
        } finally {
            setLoading(false);
        }
    }, [entityType, entityId, addToast]);

    useEffect(() => { fetchComments(); }, [fetchComments]);

    const handleCreate = async (content: string) => {
        try {
            await workApi.createComment({ entityType, entityId, content });
            await fetchComments();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to post comment');
        }
    };

    const handleReply = async (parentCommentId: string, content: string) => {
        try {
            await workApi.createComment({ entityType, entityId, content, parentCommentId });
            await fetchComments();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to post reply');
        }
    };

    const handleEdit = async (commentId: string, content: string) => {
        try {
            await workApi.updateComment(commentId, { content });
            await fetchComments();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to update comment');
        }
    };

    const handleDelete = async () => {
        if (!deleteTarget) return;
        try {
            await workApi.deleteComment(deleteTarget);
            setDeleteTarget(null);
            await fetchComments();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to delete comment');
        }
    };

    const isAuthor = (authorId: string) => user?.userId === authorId;
    const canDelete = (authorId: string) => isAuthor(authorId) || user?.roleName === 'OrgAdmin';

    // Build threaded: top-level comments are those without parentCommentId
    const topLevel = comments.filter((c) => !c.parentCommentId);

    return (
        <section className="space-y-4">
            <h3 className="text-lg font-medium text-foreground">Comments</h3>
            <CommentInput onSubmit={handleCreate} placeholder="Write a comment..." />

            {loading ? (
                <p className="text-sm text-muted-foreground">Loading comments...</p>
            ) : topLevel.length === 0 ? (
                <p className="text-sm text-muted-foreground">No comments yet</p>
            ) : (
                <div className="space-y-3">
                    {topLevel.map((comment) => (
                        <CommentItem
                            key={comment.commentId}
                            comment={comment}
                            isAuthor={isAuthor(comment.authorId)}
                            canDelete={canDelete(comment.authorId)}
                            onEdit={handleEdit}
                            onDelete={(id) => setDeleteTarget(id)}
                            onReply={handleReply}
                            isAuthorFn={isAuthor}
                            canDeleteFn={canDelete}
                        />
                    ))}
                </div>
            )}

            <ConfirmDialog
                open={deleteTarget !== null}
                onConfirm={handleDelete}
                onCancel={() => setDeleteTarget(null)}
                title="Delete Comment"
                message="Are you sure you want to delete this comment? This action cannot be undone."
                confirmLabel="Delete"
            />
        </section>
    );
}
