import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate } from 'react-router-dom';
import { securityApi } from '@/api/securityApi';
import { useAuthStore } from '@/stores/authStore';
import { useOrgStore } from '@/stores/orgStore';
import { FormField } from '@/components/forms/FormField';
import { PasswordInput } from '@/components/forms/PasswordInput';
import { passwordSchema, type PasswordFormData } from '../schemas';
import { ApiError } from '@/types/api';
import { mapErrorCode } from '@/utils/errorMapping';

export function ForcedPasswordChangePage() {
    const navigate = useNavigate();
    const setUser = useAuthStore((s) => s.setUser);
    const user = useAuthStore((s) => s.user);
    const accessToken = useAuthStore((s) => s.accessToken);
    const refreshOrg = useOrgStore((s) => s.refresh);
    const [serverError, setServerError] = useState('');

    const {
        register,
        handleSubmit,
        watch,
        formState: { errors, isSubmitting },
    } = useForm<PasswordFormData>({
        resolver: zodResolver(passwordSchema),
        defaultValues: { newPassword: '', confirmPassword: '' },
    });

    const newPasswordValue = watch('newPassword');

    const onSubmit = async (data: PasswordFormData) => {
        setServerError('');
        try {
            await securityApi.forcedPasswordChange({
                newPassword: data.newPassword,
                confirmPassword: data.confirmPassword,
            });

            // Update user to no longer be first-time
            if (user && accessToken) {
                const updatedUser = { ...user, isFirstTimeUser: false };
                setUser(updatedUser);
            }

            // Fetch org data for non-platform-admin users
            if (user?.organizationId) {
                refreshOrg();
            }

            // PlatformAdmin goes to admin panel, regular users go to home
            const isPlatformAdmin = !user?.organizationId && user?.roleName === 'PlatformAdmin';
            navigate(isPlatformAdmin ? '/admin/organizations' : '/', { replace: true });
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
            <h2 className="text-lg font-semibold text-card-foreground">Change Your Password</h2>
            <p className="text-sm text-muted-foreground">
                You must set a new password before continuing.
            </p>

            {serverError && (
                <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive" role="alert">
                    {serverError}
                </div>
            )}

            <FormField name="newPassword" label="New Password" error={errors.newPassword?.message} required>
                <PasswordInput
                    id="newPassword"
                    autoComplete="new-password"
                    placeholder="Enter new password"
                    showStrength
                    value={newPasswordValue}
                    {...register('newPassword')}
                />
            </FormField>

            <FormField name="confirmPassword" label="Confirm Password" error={errors.confirmPassword?.message} required>
                <PasswordInput
                    id="confirmPassword"
                    autoComplete="new-password"
                    placeholder="Confirm new password"
                    {...register('confirmPassword')}
                />
            </FormField>

            <button
                type="submit"
                disabled={isSubmitting}
                className="h-9 w-full rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
                {isSubmitting ? 'Changing password…' : 'Change Password'}
            </button>
        </form>
    );
}
