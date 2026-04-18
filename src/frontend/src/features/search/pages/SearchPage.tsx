import { useState, useEffect, useCallback } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { workApi } from '@/api/workApi';
import { Badge } from '@/components/common/Badge';
import { Pagination } from '@/components/common/Pagination';
import { useToast } from '@/components/common/Toast';
import { useDebounce } from '@/hooks/useDebounce';
import { usePagination } from '@/hooks/usePagination';
import type { SearchResponse, SearchResultItem } from '@/types/work';
import { Search, FileText, ListTodo, FolderKanban } from 'lucide-react';

export function SearchPage() {
    const [searchParams, setSearchParams] = useSearchParams();
    const navigate = useNavigate();
    const { addToast } = useToast();
    const { page, pageSize, setPage, setPageSize } = usePagination();

    const urlQuery = searchParams.get('q') ?? '';
    const [query, setQuery] = useState(urlQuery);
    const [entityType, setEntityType] = useState('');
    const [results, setResults] = useState<SearchResponse | null>(null);
    const [loading, setLoading] = useState(false);

    const debouncedQuery = useDebounce(query, 300);

    const doSearch = useCallback(async (q: string) => {
        if (q.length < 2) {
            setResults(null);
            return;
        }
        setLoading(true);
        try {
            const data = await workApi.search({
                query: q,
                entityType: entityType || undefined,
                page,
                pageSize,
            });
            setResults(data);
        } catch {
            addToast('error', 'Search failed');
        } finally {
            setLoading(false);
        }
    }, [entityType, page, pageSize, addToast]);

    useEffect(() => {
        if (debouncedQuery !== urlQuery) {
            setSearchParams({ q: debouncedQuery }, { replace: true });
        }
        doSearch(debouncedQuery);
    }, [debouncedQuery, doSearch, urlQuery, setSearchParams]);

    const handleResultClick = (item: SearchResultItem) => {
        if (item.entityType === 'Story') navigate(`/stories/${item.id}`);
        else if (item.entityType === 'Project') navigate(`/projects/${item.id}`);
    };

    const grouped = results?.items.reduce<Record<string, SearchResultItem[]>>((acc, item) => {
        const type = item.entityType;
        if (!acc[type]) acc[type] = [];
        acc[type].push(item);
        return acc;
    }, {}) ?? {};

    return (
        <div className="space-y-4">
            <h1 className="text-2xl font-semibold text-foreground">Search</h1>

            {/* Search input */}
            <div className="flex gap-3">
                <div className="relative flex-1">
                    <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
                    <input
                        type="text"
                        value={query}
                        onChange={(e) => setQuery(e.target.value)}
                        placeholder="Search stories, projects, tasks..."
                        className="h-10 w-full rounded-md border border-input bg-background pl-9 pr-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                        autoFocus
                    />
                </div>
                <select
                    value={entityType}
                    onChange={(e) => { setEntityType(e.target.value); setPage(1); }}
                    className="h-10 rounded-md border border-input bg-background px-3 text-sm text-foreground"
                >
                    <option value="">All Types</option>
                    <option value="Story">Stories</option>
                    <option value="Project">Projects</option>
                    <option value="Task">Tasks</option>
                </select>
            </div>

            {query.length > 0 && query.length < 2 && (
                <p className="text-sm text-muted-foreground">Enter at least 2 characters to search</p>
            )}

            {loading && <p className="text-sm text-muted-foreground">Searching...</p>}

            {!loading && results && results.items.length === 0 && (
                <p className="text-sm text-muted-foreground">No results found for "{debouncedQuery}"</p>
            )}

            {!loading && results && results.items.length > 0 && (
                <div className="space-y-4">
                    {Object.entries(grouped).map(([type, items]) => (
                        <section key={type} className="space-y-2">
                            <h2 className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                                {type === 'Story' ? <FileText size={14} /> : type === 'Project' ? <FolderKanban size={14} /> : <ListTodo size={14} />}
                                {type}s ({items.length})
                            </h2>
                            <div className="space-y-1.5">
                                {items.map((item) => (
                                    <button
                                        key={item.id}
                                        onClick={() => handleResultClick(item)}
                                        className="flex w-full items-center justify-between rounded-md border border-border p-3 text-left hover:bg-muted/50"
                                    >
                                        <div className="min-w-0 flex-1">
                                            <div className="flex items-center gap-2">
                                                {item.storyKey && <span className="text-xs font-medium text-muted-foreground">{item.storyKey}</span>}
                                                <span className="text-sm font-medium text-foreground truncate">{item.title}</span>
                                            </div>
                                            <div className="mt-1 flex items-center gap-2">
                                                {item.assigneeName && <span className="text-xs text-muted-foreground">{item.assigneeName}</span>}
                                                {item.departmentName && <span className="text-xs text-muted-foreground">· {item.departmentName}</span>}
                                            </div>
                                        </div>
                                        <div className="flex items-center gap-2 shrink-0">
                                            <Badge variant="status" value={item.status} />
                                            <Badge variant="priority" value={item.priority} />
                                        </div>
                                    </button>
                                ))}
                            </div>
                        </section>
                    ))}

                    <Pagination
                        page={results.page}
                        pageSize={results.pageSize}
                        totalCount={results.totalCount}
                        onPageChange={setPage}
                        onPageSizeChange={setPageSize}
                    />
                </div>
            )}
        </div>
    );
}
