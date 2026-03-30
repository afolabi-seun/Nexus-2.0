import { Link } from 'react-router-dom';

export function NotFoundPage() {
    return (
        <div className="min-h-screen flex flex-col items-center justify-center bg-background text-foreground">
            <h1 className="text-4xl font-bold mb-2">404</h1>
            <p className="text-muted-foreground mb-4">Page not found</p>
            <Link to="/" className="text-primary underline">
                Go to Dashboard
            </Link>
        </div>
    );
}
