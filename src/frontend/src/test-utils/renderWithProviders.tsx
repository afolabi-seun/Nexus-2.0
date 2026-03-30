import { render, type RenderOptions, type RenderResult } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { ReactElement } from 'react';

interface ProviderOptions extends Omit<RenderOptions, 'wrapper'> {
    route?: string;
}

export function renderWithProviders(
    ui: ReactElement,
    options: ProviderOptions = {}
): RenderResult {
    const { route = '/', ...renderOptions } = options;

    function Wrapper({ children }: { children: React.ReactNode }) {
        return (
            <MemoryRouter initialEntries={[route]}>
                {children}
            </MemoryRouter>
        );
    }

    return render(ui, { wrapper: Wrapper, ...renderOptions });
}
