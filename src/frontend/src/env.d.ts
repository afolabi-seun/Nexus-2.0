/// <reference types="vite/client" />

interface ImportMetaEnv {
    readonly VITE_SECURITY_API_URL: string;
    readonly VITE_PROFILE_API_URL: string;
    readonly VITE_WORK_API_URL: string;
    readonly VITE_UTILITY_API_URL: string;
    readonly VITE_APP_NAME: string;
}

interface ImportMeta {
    readonly env: ImportMetaEnv;
}
