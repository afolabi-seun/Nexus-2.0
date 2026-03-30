import { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';

export function StoryByKeyRedirect() {
    const { key } = useParams<{ key: string }>();
    const navigate = useNavigate();
    const { addToast } = useToast();

    useEffect(() => {
        if (!key) {
            navigate('/stories', { replace: true });
            return;
        }
        workApi
            .getStoryByKey(key)
            .then((story) => {
                navigate(`/stories/${story.storyId}`, { replace: true });
            })
            .catch(() => {
                addToast('error', `Story "${key}" not found`);
                navigate('/stories', { replace: true });
            });
    }, [key, navigate, addToast]);

    return <SkeletonLoader variant="form" />;
}
