const REQUIRED_VARS = [
    'VITE_SECURITY_API_URL',
    'VITE_PROFILE_API_URL',
    'VITE_WORK_API_URL',
    'VITE_UTILITY_API_URL',
    'VITE_BILLING_API_URL',
] as const;

function validateEnv(): void {
    const missing = REQUIRED_VARS.filter(
        (key) => !import.meta.env[key]
    );
    if (missing.length > 0) {
        throw new Error(
            `Missing required environment variables: ${missing.join(', ')}. ` +
            'Copy .env.example to .env and fill in the values.'
        );
    }
}

export const env = {
    SECURITY_API_URL: import.meta.env.VITE_SECURITY_API_URL ?? '',
    PROFILE_API_URL: import.meta.env.VITE_PROFILE_API_URL ?? '',
    WORK_API_URL: import.meta.env.VITE_WORK_API_URL ?? '',
    UTILITY_API_URL: import.meta.env.VITE_UTILITY_API_URL ?? '',
    BILLING_API_URL: import.meta.env.VITE_BILLING_API_URL ?? '',
    APP_NAME: import.meta.env.VITE_APP_NAME ?? 'Nexus 2.0',
} as const;

export { validateEnv };
