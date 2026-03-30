import { create } from 'zustand';

type ThemeSetting = 'Light' | 'Dark' | 'System';
type ResolvedTheme = 'Light' | 'Dark';

interface ThemeState {
    theme: ThemeSetting;
    resolvedTheme: ResolvedTheme;
}

interface ThemeActions {
    setTheme(theme: ThemeSetting): void;
}

function getSystemTheme(): ResolvedTheme {
    if (typeof window === 'undefined') return 'Light';
    return window.matchMedia('(prefers-color-scheme: dark)').matches
        ? 'Dark'
        : 'Light';
}

function resolveTheme(theme: ThemeSetting): ResolvedTheme {
    if (theme === 'System') return getSystemTheme();
    return theme;
}

function applyThemeToDOM(resolved: ResolvedTheme): void {
    if (typeof document === 'undefined') return;
    const html = document.documentElement;
    if (resolved === 'Dark') {
        html.classList.add('dark');
    } else {
        html.classList.remove('dark');
    }
}

function loadPersistedTheme(): ThemeSetting {
    try {
        const stored = localStorage.getItem('nexus_theme');
        if (stored === 'Light' || stored === 'Dark' || stored === 'System') {
            return stored;
        }
    } catch {
        // ignore
    }
    return 'System';
}

const initialTheme = loadPersistedTheme();
const initialResolved = resolveTheme(initialTheme);
applyThemeToDOM(initialResolved);

export const useThemeStore = create<ThemeState & ThemeActions>()((set) => {
    // Listen for OS theme changes when in System mode
    if (typeof window !== 'undefined') {
        const mq = window.matchMedia('(prefers-color-scheme: dark)');
        mq.addEventListener('change', () => {
            const state = useThemeStore.getState();
            if (state.theme === 'System') {
                const resolved = getSystemTheme();
                applyThemeToDOM(resolved);
                set({ resolvedTheme: resolved });
            }
        });
    }

    return {
        theme: initialTheme,
        resolvedTheme: initialResolved,

        setTheme(theme) {
            try {
                localStorage.setItem('nexus_theme', theme);
            } catch {
                // ignore
            }
            const resolved = resolveTheme(theme);
            applyThemeToDOM(resolved);
            set({ theme, resolvedTheme: resolved });
        },
    };
});
