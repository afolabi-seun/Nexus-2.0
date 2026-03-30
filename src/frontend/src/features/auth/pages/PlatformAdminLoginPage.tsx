import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate } from 'react-router-dom';
import { securityApi } from '@/api/securityApi';
import { useAuthStore, extractUserFromToken } from '@/stores/authStore';
import { FormField } from '@/components/forms/FormField';
import { PasswordInput } from '@/components/forms/PasswordInput';
import { platformAdminLoginSchema, type PlatformAdminLoginFormData } from '../schemas';
import { ApiError } from '@/types/api';
import { mapErrorCode } from '@/utils/errorMapping';

export function PlatformAdminLoginPage() {
    const navigate = useNavigate();
    const login = useAuthStore((s) => s.login);
    const [serverError, setServerError] = useState('');

    const {
        register,
        handleSubmit,
        formState: { errors, isSubmitting },
    } = useForm<PlatformAdminLoginFormData>({
        resolver: zodResolver(platformAdminLoginSchema),
        defaultValues: { username: '', password: '' },
    });

    const onSubmit = async (data: PlatformAdminLoginFormData) => {
        setServerError('');
        try {
            // Send username in the email field for platform admin login
            const response = await securityApi.login({
                email: data.username,
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

            // PlatformAdmin: no organizationId, roleName=PlatformAdmin
            navigate('/admin/organizations', { replace: true });
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
            <h2 className="text-lg font-semibold text-card-foreground">Platform Admin Login</h2>

            {serverError && (
                <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive" role="alert">
                    {serverError}
                </div>
            )}

            <FormField name="username" label="Username" error={errors.username?.message} required>
                <input
                    id="username"
                    type="text"
                    autoComplete="username"
                    className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    placeholder="Enter your username"
                    {...register('username')}
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
        </form>
    );
}
