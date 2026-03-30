import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { securityApi } from '@/api/securityApi';
import { useToast } from '@/components/common/Toast';
import { FormField } from '@/components/forms/FormField';
import { PasswordInput } from '@/components/forms/PasswordInput';
import { OtpInput } from '@/components/forms/OtpInput';
import { passwordSchema } from '../schemas';
import { ApiError } from '@/types/api';
import { mapErrorCode } from '@/utils/errorMapping';

const emailStepSchema = z.object({
    email: z.string().min(1, 'Email is required').email('Invalid email address'),
});

type EmailStepData = z.infer<typeof emailStepSchema>;

const confirmStepSchema = passwordSchema;
type ConfirmStepData = z.infer<typeof confirmStepSchema>;

export function PasswordResetPage() {
    const navigate = useNavigate();
    const { addToast } = useToast();
    const [step, setStep] = useState<1 | 2>(1);
    const [email, setEmail] = useState('');
    const [otp, setOtp] = useState('');
    const [serverError, setServerError] = useState('');

    // Step 1 form
    const emailForm = useForm<EmailStepData>({
        resolver: zodResolver(emailStepSchema),
        defaultValues: { email: '' },
    });

    // Step 2 form
    const confirmForm = useForm<ConfirmStepData>({
        resolver: zodResolver(confirmStepSchema),
        defaultValues: { newPassword: '', confirmPassword: '' },
    });

    const newPasswordValue = confirmForm.watch('newPassword');

    const handleEmailSubmit = async (data: EmailStepData) => {
        setServerError('');
        try {
            await securityApi.requestPasswordReset({ email: data.email });
            setEmail(data.email);
            setStep(2);
        } catch (err) {
            if (err instanceof ApiError) {
                setServerError(mapErrorCode(err.errorCode));
            } else {
                setServerError('Something went wrong. Please try again.');
            }
        }
    };

    const handleResendCode = async () => {
        setServerError('');
        try {
            await securityApi.requestPasswordReset({ email });
            addToast('success', 'A new code has been sent to your email.');
        } catch (err) {
            if (err instanceof ApiError) {
                setServerError(mapErrorCode(err.errorCode));
            } else {
                setServerError('Something went wrong. Please try again.');
            }
        }
    };

    const handleConfirmSubmit = async (data: ConfirmStepData) => {
        if (!otp || otp.length !== 6) {
            setServerError('Please enter the 6-digit code.');
            return;
        }
        setServerError('');
        try {
            await securityApi.confirmPasswordReset({
                email,
                otp,
                newPassword: data.newPassword,
                confirmPassword: data.confirmPassword,
            });
            addToast('success', 'Password reset successfully. Please sign in.');
            navigate('/login', { replace: true });
        } catch (err) {
            if (err instanceof ApiError) {
                setServerError(mapErrorCode(err.errorCode));
            } else {
                setServerError('Something went wrong. Please try again.');
            }
        }
    };

    if (step === 1) {
        return (
            <form onSubmit={emailForm.handleSubmit(handleEmailSubmit)} className="space-y-4" noValidate>
                <h2 className="text-lg font-semibold text-card-foreground">Reset Password</h2>
                <p className="text-sm text-muted-foreground">
                    Enter your email and we'll send you a verification code.
                </p>

                {serverError && (
                    <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive" role="alert">
                        {serverError}
                    </div>
                )}

                <FormField name="email" label="Email" error={emailForm.formState.errors.email?.message} required>
                    <input
                        id="email"
                        type="email"
                        autoComplete="email"
                        className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                        placeholder="you@example.com"
                        {...emailForm.register('email')}
                    />
                </FormField>

                <button
                    type="submit"
                    disabled={emailForm.formState.isSubmitting}
                    className="h-9 w-full rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                >
                    {emailForm.formState.isSubmitting ? 'Sending…' : 'Send Code'}
                </button>
            </form>
        );
    }

    return (
        <form onSubmit={confirmForm.handleSubmit(handleConfirmSubmit)} className="space-y-4" noValidate>
            <h2 className="text-lg font-semibold text-card-foreground">Enter Verification Code</h2>
            <p className="text-sm text-muted-foreground">
                We sent a 6-digit code to <span className="font-medium">{email}</span>.
            </p>

            {serverError && (
                <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive" role="alert">
                    {serverError}
                    {serverError.includes('expired') && (
                        <button
                            type="button"
                            onClick={handleResendCode}
                            className="ml-2 font-medium underline hover:no-underline"
                        >
                            Resend Code
                        </button>
                    )}
                </div>
            )}

            <div className="space-y-1.5">
                <label className="block text-sm font-medium text-foreground">Verification Code</label>
                <OtpInput onComplete={setOtp} />
            </div>

            <FormField name="newPassword" label="New Password" error={confirmForm.formState.errors.newPassword?.message} required>
                <PasswordInput
                    id="newPassword"
                    autoComplete="new-password"
                    placeholder="Enter new password"
                    showStrength
                    value={newPasswordValue}
                    {...confirmForm.register('newPassword')}
                />
            </FormField>

            <FormField name="confirmPassword" label="Confirm Password" error={confirmForm.formState.errors.confirmPassword?.message} required>
                <PasswordInput
                    id="confirmPassword"
                    autoComplete="new-password"
                    placeholder="Confirm new password"
                    {...confirmForm.register('confirmPassword')}
                />
            </FormField>

            <button
                type="submit"
                disabled={confirmForm.formState.isSubmitting}
                className="h-9 w-full rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
                {confirmForm.formState.isSubmitting ? 'Resetting…' : 'Reset Password'}
            </button>

            <div className="text-center">
                <button
                    type="button"
                    onClick={handleResendCode}
                    className="text-sm text-primary hover:underline"
                >
                    Resend Code
                </button>
            </div>
        </form>
    );
}
