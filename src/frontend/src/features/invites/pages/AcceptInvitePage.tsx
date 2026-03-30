import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { profileApi } from '@/api/profileApi';
import { OtpInput } from '@/components/forms/OtpInput';
import { useToast } from '@/components/common/Toast';
import { SkeletonLoader } from '@/components/common/SkeletonLoader';
import { mapErrorCode } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';
import type { InviteValidation } from '@/types/profile';
import { CheckCircle2, AlertCircle } from 'lucide-react';

export function AcceptInvitePage() {
    const { token } = useParams<{ token: string }>();
    const navigate = useNavigate();
    const { addToast } = useToast();

    const [validation, setValidation] = useState<InviteValidation | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [accepting, setAccepting] = useState(false);
    const [accepted, setAccepted] = useState(false);

    useEffect(() => {
        if (!token) return;
        profileApi.validateInvite(token)
            .then(setValidation)
            .catch((err) => {
                if (err instanceof ApiError) setError(mapErrorCode(err.errorCode));
                else setError('This invitation is no longer valid.');
            })
            .finally(() => setLoading(false));
    }, [token]);

    const handleOtpComplete = async (otp: string) => {
        if (!token) return;
        setAccepting(true);
        try {
            await profileApi.acceptInvite(token, { otp });
            setAccepted(true);
            addToast('success', 'Invitation accepted! You can now log in.');
            setTimeout(() => navigate('/login', { replace: true }), 2000);
        } catch (err) {
            if (err instanceof ApiError) addToast('error', mapErrorCode(err.errorCode));
            else addToast('error', 'Failed to accept invitation');
        } finally {
            setAccepting(false);
        }
    };

    if (loading) return <SkeletonLoader variant="form" />;

    if (error) {
        return (
            <div className="flex min-h-[400px] items-center justify-center">
                <div className="text-center space-y-3">
                    <AlertCircle size={48} className="mx-auto text-destructive" />
                    <h1 className="text-xl font-semibold text-foreground">Invalid Invitation</h1>
                    <p className="text-sm text-muted-foreground">{error}</p>
                    <button onClick={() => navigate('/login')} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90">
                        Go to Login
                    </button>
                </div>
            </div>
        );
    }

    if (accepted) {
        return (
            <div className="flex min-h-[400px] items-center justify-center">
                <div className="text-center space-y-3">
                    <CheckCircle2 size={48} className="mx-auto text-green-500" />
                    <h1 className="text-xl font-semibold text-foreground">Welcome!</h1>
                    <p className="text-sm text-muted-foreground">Redirecting to login...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="flex min-h-[400px] items-center justify-center">
            <div className="w-full max-w-md space-y-6 rounded-lg border border-border bg-card p-8">
                <div className="text-center space-y-2">
                    <h1 className="text-2xl font-semibold text-card-foreground">Accept Invitation</h1>
                    {validation && (
                        <div className="space-y-1">
                            <p className="text-sm text-muted-foreground">
                                You've been invited to join <span className="font-medium text-foreground">{validation.organizationName}</span>
                            </p>
                            <p className="text-sm text-muted-foreground">
                                Department: <span className="font-medium text-foreground">{validation.departmentName}</span> · Role: <span className="font-medium text-foreground">{validation.roleName}</span>
                            </p>
                        </div>
                    )}
                </div>

                <div className="space-y-3">
                    <p className="text-center text-sm text-muted-foreground">Enter the verification code sent to your email</p>
                    <OtpInput onComplete={handleOtpComplete} disabled={accepting} />
                    {accepting && <p className="text-center text-sm text-muted-foreground">Verifying...</p>}
                </div>
            </div>
        </div>
    );
}
