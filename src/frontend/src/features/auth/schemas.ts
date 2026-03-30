import { z } from 'zod';

export const loginSchema = z.object({
    email: z.string().min(1, 'Email is required').email('Invalid email address'),
    password: z.string().min(1, 'Password is required'),
});

export type LoginFormData = z.infer<typeof loginSchema>;

export const passwordSchema = z
    .object({
        newPassword: z
            .string()
            .min(8, 'Password must be at least 8 characters')
            .regex(/[A-Z]/, 'Must contain at least 1 uppercase letter')
            .regex(/[a-z]/, 'Must contain at least 1 lowercase letter')
            .regex(/\d/, 'Must contain at least 1 digit')
            .regex(/[!@#$%^&*]/, 'Must contain at least 1 special character (!@#$%^&*)'),
        confirmPassword: z.string().min(1, 'Please confirm your password'),
    })
    .refine((data) => data.newPassword === data.confirmPassword, {
        message: 'Passwords do not match',
        path: ['confirmPassword'],
    });

export type PasswordFormData = z.infer<typeof passwordSchema>;

export const otpSchema = z.object({
    otp: z.string().length(6, 'OTP must be exactly 6 digits').regex(/^\d{6}$/, 'OTP must be exactly 6 digits'),
});

export type OtpFormData = z.infer<typeof otpSchema>;

export const platformAdminLoginSchema = z.object({
    username: z.string().min(1, 'Username is required'),
    password: z.string().min(1, 'Password is required'),
});

export type PlatformAdminLoginFormData = z.infer<typeof platformAdminLoginSchema>;
