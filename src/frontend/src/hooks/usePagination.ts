import { useState, useCallback } from 'react';

interface PaginationState {
    page: number;
    pageSize: number;
}

interface UsePaginationReturn extends PaginationState {
    setPage: (page: number) => void;
    setPageSize: (size: number) => void;
    totalPages: (totalCount: number) => number;
    reset: () => void;
}

export function usePagination(
    initialPage = 1,
    initialPageSize = 10
): UsePaginationReturn {
    const [state, setState] = useState<PaginationState>({
        page: initialPage,
        pageSize: initialPageSize,
    });

    const setPage = useCallback((page: number) => {
        setState((prev) => ({ ...prev, page }));
    }, []);

    const setPageSize = useCallback((pageSize: number) => {
        setState({ page: 1, pageSize });
    }, []);

    const totalPages = useCallback(
        (totalCount: number) => Math.ceil(totalCount / state.pageSize),
        [state.pageSize]
    );

    const reset = useCallback(() => {
        setState({ page: initialPage, pageSize: initialPageSize });
    }, [initialPage, initialPageSize]);

    return {
        page: state.page,
        pageSize: state.pageSize,
        setPage,
        setPageSize,
        totalPages,
        reset,
    };
}
