import { useState, useEffect } from 'react';

export function useMediaQuery(query: string): boolean {
    const [matches, setMatches] = useState(() => {
        if (typeof window === 'undefined') return false;
        return window.matchMedia(query).matches;
    });

    useEffect(() => {
        const mq = window.matchMedia(query);
        const handler = (e: MediaQueryListEvent) => setMatches(e.matches);
        mq.addEventListener('change', handler);
        setMatches(mq.matches);
        return () => mq.removeEventListener('change', handler);
    }, [query]);

    return matches;
}

// Convenience breakpoints matching Tailwind defaults
export function useIsMobile(): boolean {
    return !useMediaQuery('(min-width: 768px)');
}

export function useIsDesktop(): boolean {
    return useMediaQuery('(min-width: 1024px)');
}
