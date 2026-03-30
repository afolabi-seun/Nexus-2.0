import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { useToast } from '@/components/common/Toast';
import { Modal } from '@/components/common/Modal';
import { Badge } from '@/components/common/Badge';
import { useDebounce } from '@/hooks/useDebounce';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import { LinkType } from '@/types/enums';
import type { StoryLink, StoryListItem } from '@/types/work';
import { Link2, X, Search, Plus } from 'lucide-react';
import { useEffect } from 'react';

const LINK_TYPE_LABELS: Record<LinkType, string> = {
    [LinkType.Blocks]: 'Blocks',
    [LinkType.IsBlockedBy]: 'Is Blocked By',
    [LinkType.RelatesTo]: 'Relates To',
    [LinkType.Duplicates]: 'Duplicates',
};

interface StoryLinkDialogProps {
    storyId: string;
    links: StoryLink[];
    onLinksChanged: () => void;
}

export function StoryLinkDialog({ storyId, links, onLinksChanged }: StoryLinkDialogProps) {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const [dialogOpen, setDialogOpen] = useState(false);
    const [searchQuery, setSearchQuery] = useState('');
    const [searchResults, setSearchResults] = useState<StoryListItem[]>([]);
    const [selectedStoryId, setSelectedStoryId] = useState('');
    const [selectedStoryKey, setSelectedStoryKey] = useState('');
    const [linkType, setLinkType] = useState<LinkType>(LinkType.RelatesTo);
    const [creating, setCreating] = useState(false);
    const [searching, setSearching] = useState(false);
    const debouncedQuery = useDebounce(searchQuery, 300);

    useEffect(() => {
        if (!debouncedQuery.trim()) { setSearchResults([]); return; }
        let cancelled = false;
        setSearching(true);
        workApi.getStories({ page: 1, pageSize: 10 }).then((res) => {
            if (!cancelled) {
                setSearchResults(
                    res.data.filter(
                        (s) =>
                            s.storyId !== storyId &&
                            (s.storyKey.toLowerCase().includes(debouncedQuery.toLowerCase()) ||
                                s.title.toLowerCase().includes(debouncedQuery.toLowerCase()))
                    )
                );
            }
        }).catch(() => {
            if (!cancelled) setSearchResults([]);
        }).finally(() => {
            if (!cancelled) setSearching(false);
        });
        return () => { cancelled = true; };
    }, [debouncedQuery, storyId]);

    const handleCreateLink = async () => {
        if (!selectedStoryId) return;
        setCreating(true);
        try {
            await workApi.createStoryLink(storyId, { targetStoryId: selectedStoryId, linkType });
            addToast('success', 'Link created');
            setSelectedStoryId('');
            setSelectedStoryKey('');
            setSearchQuery('');
            setDialogOpen(false);
            onLinksChanged();
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to create link');
        } finally {
            setCreating(false);
        }
    };

    const handleRemoveLink = async (linkId: string) => {
        try {
            await workApi.removeStoryLink(storyId, linkId);
            addToast('success', 'Link removed');
            onLinksChanged();
        } catch {
            addToast('error', 'Failed to remove link');
        }
    };

    const inputClass = 'h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring';

    return (
        <div className="space-y-3">
            {/* Linked stories list */}
            {links.length > 0 && (
                <ul className="space-y-1.5">
                    {links.map((link) => (
                        <li key={link.linkId} className="flex items-center justify-between rounded-md border border-border px-3 py-2">
                            <div className="flex items-center gap-2">
                                <Link2 size={14} className="text-muted-foreground" />
                                <button
                                    onClick={() => navigate(`/stories/${link.targetStoryId}`)}
                                    className="text-sm font-medium text-primary hover:underline"
                                >
                                    {link.targetStoryKey}
                                </button>
                                <span className="text-sm text-foreground truncate max-w-[200px]">{link.targetStoryTitle}</span>
                                <Badge value={LINK_TYPE_LABELS[link.linkType]} />
                            </div>
                            <button
                                onClick={() => handleRemoveLink(link.linkId)}
                                className="rounded p-1 text-muted-foreground hover:text-destructive"
                                aria-label="Remove link"
                            >
                                <X size={14} />
                            </button>
                        </li>
                    ))}
                </ul>
            )}

            <button
                onClick={() => { setDialogOpen(true); setSelectedStoryId(''); setSelectedStoryKey(''); setSearchQuery(''); }}
                className="inline-flex items-center gap-1 rounded-md border border-input px-2 py-1 text-xs font-medium text-foreground hover:bg-accent"
            >
                <Plus size={12} /> Link Story
            </button>

            <Modal open={dialogOpen} onClose={() => setDialogOpen(false)} title="Link Story">
                <div className="space-y-4">
                    {/* Story search */}
                    <div className="relative">
                        <label className="mb-1 block text-sm font-medium text-foreground">Search Story</label>
                        <div className="relative">
                            <Search size={14} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
                            <input
                                type="text"
                                value={selectedStoryKey || searchQuery}
                                onChange={(e) => { setSearchQuery(e.target.value); setSelectedStoryId(''); setSelectedStoryKey(''); }}
                                placeholder="Search by key or title..."
                                className={`${inputClass} pl-8`}
                            />
                        </div>
                        {searchQuery.trim() && !selectedStoryId && (
                            <ul className="absolute z-50 mt-1 max-h-40 w-full overflow-auto rounded-md border border-border bg-popover py-1 shadow-lg">
                                {searching && <li className="px-3 py-2 text-sm text-muted-foreground">Searching...</li>}
                                {!searching && searchResults.length === 0 && (
                                    <li className="px-3 py-2 text-sm text-muted-foreground">No stories found</li>
                                )}
                                {searchResults.map((s) => (
                                    <li key={s.storyId}>
                                        <button
                                            type="button"
                                            onClick={() => {
                                                setSelectedStoryId(s.storyId);
                                                setSelectedStoryKey(s.storyKey);
                                                setSearchQuery('');
                                            }}
                                            className="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-accent text-popover-foreground"
                                        >
                                            <span className="font-medium">{s.storyKey}</span>
                                            <span className="truncate text-muted-foreground">{s.title}</span>
                                        </button>
                                    </li>
                                ))}
                            </ul>
                        )}
                    </div>

                    {/* Link type */}
                    <div>
                        <label className="mb-1 block text-sm font-medium text-foreground">Link Type</label>
                        <select
                            value={linkType}
                            onChange={(e) => setLinkType(e.target.value as LinkType)}
                            className={inputClass}
                        >
                            {Object.entries(LINK_TYPE_LABELS).map(([key, label]) => (
                                <option key={key} value={key}>{label}</option>
                            ))}
                        </select>
                    </div>

                    <div className="flex justify-end gap-2 pt-2">
                        <button
                            type="button"
                            onClick={() => setDialogOpen(false)}
                            className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent"
                        >
                            Cancel
                        </button>
                        <button
                            onClick={handleCreateLink}
                            disabled={!selectedStoryId || creating}
                            className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                        >
                            {creating ? 'Linking...' : 'Create Link'}
                        </button>
                    </div>
                </div>
            </Modal>
        </div>
    );
}
