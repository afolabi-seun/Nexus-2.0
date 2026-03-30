import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router-dom';
import { securityApi } from '@/api/securityApi';
import { useAuthStore, extractUserFromToken } from '@/stores/authStore';
import { useOrgStore } from '@/stores/orgStore';
import { FormField } from '@/components/forms/FormField';
import { PasswordInput } from '@/components/forms/PasswordInput';
import { loginSchema, type LoginFormData } from '../schemas';
import { ApiError } from '@/types/api';
import { mapErrorCode } from '@/utils/errorMapping';

export function LoginPage() {
    const navigate = useNavigate();
    const login = useAuthStore((s) => s.login);
    const refreshOrg = useOrgStore((s) => s.refresh);
    const [serverError, setServerError] = useState('');

    const {
        register,
        handleSubmit,
        formState: { errors, isSubmitting },
    } = useForm<LoginFormData>({
        resolver: zodResolver(loginSchema),
        defaultValues: { email: '', password: '' },
    });

    const onSubmit = async (data: LoginFormData) => {
        setServerError('');
        try {
            const response = await securityApi.login({
                email: data.email,
                password: data.password,
            });

            const user = extractUserFromToken(response.accessToken);
            login(
                { accessToken: response.accessToken, refreshToken: response.refreshToken },
                user
            );

            if (response.isFirstTimeUser || user.isFirstTimeUser) {
                navigate('/password/change', { replace: true });
                return;
            }

            // Fetch org data in background for non-platform-admin users
            if (user.organizationId) {
                refreshOrg();
            }

            const redirect = sessionStorage.getItem('nexus_redirect') || '/';
            sessionStorage.removeItem('nexus_redirect');
            navigate(redirect, { replace: true });
        } catch (err) {
            if (err instanceof ApiError) {
                setServerError(mapErrorCode(err.errorCode));
            } else {
                setServerError('Something went wrong. Please try again.');
            }
        }
    };

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <h2 className="text-lg font-semibold text-card-foreground">Sign in to your account</h2>

            {serverError && (
                <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive" role="alert">
                    {serverError}
                </div>
            )}

            <FormField name="email" label="Email" error={errors.email?.message} required>
                <input
                    id="email"
                    type="email"
                    autoComplete="email"
                    className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    placeholder="you@example.com"
                    {...register('email')}
                />
            </FormField>

            <FormField name="password" label="Password" error={errors.password?.message} required>
                <PasswordInput
                    id="password"
                    autoComplete="current-password"
                    placeholder="Enter your password"
                    {...register('password')}
                />
            </FormField>

            <button
                type="submit"
                disabled={isSubmitting}
                className="h-9 w-full rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
                {isSubmitting ? 'Signing in…' : 'Login'}
            </button>

            <div className="text-center">
                <Link
                    to="/password/reset"
                    className="text-sm text-primary hover:underline"
                >
                    Forgot Password?
                </Link>
            </div>
        </form>
    );
}
