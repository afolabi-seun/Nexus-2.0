import { useState, useEffect } from 'react';
import { Modal } from '@/components/common/Modal';
import { FormField } from '@/components/forms/FormField';
import type { AdminPlanResponse, AdminCreatePlanRequest, AdminUpdatePlanRequest } from '@/types/adminBilling';

interface PlanFormModalProps {
    open: boolean;
    onClose: () => void;
    onCreateSubmit?: (data: AdminCreatePlanRequest) => Promise<void>;
    onEditSubmit?: (planId: string, data: AdminUpdatePlanRequest) => Promise<void>;
    plan?: AdminPlanResponse | null;
}

const emptyForm = {
    planName: '',
    planCode: '',
    tierLevel: 1,
    maxTeamMembers: 5,
    maxDepartments: 1,
    maxStoriesPerMonth: 100,
    priceMonthly: 0,
    priceYearly: 0,
    featuresJson: '',
};

export function PlanFormModal({ open, onClose, onCreateSubmit, onEditSubmit, plan }: PlanFormModalProps) {
    const isEdit = !!plan;
    const [form, setForm] = useState(emptyForm);
    const [saving, setSaving] = useState(false);
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [apiError, setApiError] = useState<string | null>(null);

    useEffect(() => {
        if (open) {
            setApiError(null);
            setErrors({});
            if (plan) {
                setForm({
                    planName: plan.planName,
                    planCode: plan.planCode,
                    tierLevel: plan.tierLevel,
                    maxTeamMembers: plan.maxTeamMembers,
                    maxDepartments: plan.maxDepartments,
                    maxStoriesPerMonth: plan.maxStoriesPerMonth,
                    priceMonthly: plan.priceMonthly,
                    priceYearly: plan.priceYearly,
                    featuresJson: plan.featuresJson ?? '',
                });
            } else {
                setForm(emptyForm);
            }
        }
    }, [open, plan]);

    const validate = (): boolean => {
        const errs: Record<string, string> = {};
        if (!form.planName.trim()) errs.planName = 'Plan name is required';
        if (!isEdit) {
            if (!form.planCode.trim()) errs.planCode = 'Plan code is required';
            else if (!/^[A-Z0-9_]{2,20}$/.test(form.planCode)) errs.planCode = 'Must be 2–20 uppercase alphanumeric/underscore characters';
        }
        if (form.tierLevel <= 0) errs.tierLevel = 'Must be a positive integer';
        if (form.maxTeamMembers <= 0) errs.maxTeamMembers = 'Must be a positive integer';
        if (form.maxDepartments <= 0) errs.maxDepartments = 'Must be a positive integer';
        if (form.maxStoriesPerMonth <= 0) errs.maxStoriesPerMonth = 'Must be a positive integer';
        if (form.priceMonthly < 0) errs.priceMonthly = 'Must be non-negative';
        if (form.priceYearly < 0) errs.priceYearly = 'Must be non-negative';
        setErrors(errs);
        return Object.keys(errs).length === 0;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!validate()) return;
        setSaving(true);
        setApiError(null);
        try {
            if (isEdit && plan && onEditSubmit) {
                await onEditSubmit(plan.planId, {
                    planName: form.planName,
                    tierLevel: form.tierLevel,
                    maxTeamMembers: form.maxTeamMembers,
                    maxDepartments: form.maxDepartments,
                    maxStoriesPerMonth: form.maxStoriesPerMonth,
                    priceMonthly: form.priceMonthly,
                    priceYearly: form.priceYearly,
                    featuresJson: form.featuresJson || undefined,
                });
            } else if (onCreateSubmit) {
                await onCreateSubmit({
                    planName: form.planName,
                    planCode: form.planCode,
                    tierLevel: form.tierLevel,
                    maxTeamMembers: form.maxTeamMembers,
                    maxDepartments: form.maxDepartments,
                    maxStoriesPerMonth: form.maxStoriesPerMonth,
                    priceMonthly: form.priceMonthly,
                    priceYearly: form.priceYearly,
                    featuresJson: form.featuresJson || undefined,
                });
            }
            onClose();
        } catch (err) {
            setApiError(err instanceof Error ? err.message : 'Failed to save plan');
        } finally {
            setSaving(false);
        }
    };

    const setField = (key: string, value: string | number) => setForm((f) => ({ ...f, [key]: value }));

    const inputClass = 'h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring';

    return (
        <Modal open={open} onClose={onClose} title={isEdit ? 'Edit Plan' : 'Create Plan'}>
            <form onSubmit={handleSubmit} className="space-y-4 max-h-[70vh] overflow-y-auto pr-1">
                {apiError && (
                    <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive" role="alert">
                        {apiError}
                    </p>
                )}
                <FormField name="planName" label="Plan Name" required error={errors.planName}>
                    <input id="planName" value={form.planName} onChange={(e) => setField('planName', e.target.value)} className={inputClass} />
                </FormField>
                <FormField name="planCode" label="Plan Code" required={!isEdit} error={errors.planCode}>
                    <input id="planCode" value={form.planCode} onChange={(e) => setField('planCode', e.target.value.toUpperCase())} disabled={isEdit} className={`${inputClass} ${isEdit ? 'opacity-50 cursor-not-allowed' : ''}`} />
                </FormField>
                <div className="grid grid-cols-2 gap-4">
                    <FormField name="tierLevel" label="Tier Level" required error={errors.tierLevel}>
                        <input id="tierLevel" type="number" value={form.tierLevel} onChange={(e) => setField('tierLevel', Number(e.target.value))} className={inputClass} />
                    </FormField>
                    <FormField name="maxTeamMembers" label="Max Team Members" required error={errors.maxTeamMembers}>
                        <input id="maxTeamMembers" type="number" value={form.maxTeamMembers} onChange={(e) => setField('maxTeamMembers', Number(e.target.value))} className={inputClass} />
                    </FormField>
                    <FormField name="maxDepartments" label="Max Departments" required error={errors.maxDepartments}>
                        <input id="maxDepartments" type="number" value={form.maxDepartments} onChange={(e) => setField('maxDepartments', Number(e.target.value))} className={inputClass} />
                    </FormField>
                    <FormField name="maxStoriesPerMonth" label="Max Stories/Month" required error={errors.maxStoriesPerMonth}>
                        <input id="maxStoriesPerMonth" type="number" value={form.maxStoriesPerMonth} onChange={(e) => setField('maxStoriesPerMonth', Number(e.target.value))} className={inputClass} />
                    </FormField>
                    <FormField name="priceMonthly" label="Monthly Price ($)" required error={errors.priceMonthly}>
                        <input id="priceMonthly" type="number" step="0.01" value={form.priceMonthly} onChange={(e) => setField('priceMonthly', Number(e.target.value))} className={inputClass} />
                    </FormField>
                    <FormField name="priceYearly" label="Yearly Price ($)" required error={errors.priceYearly}>
                        <input id="priceYearly" type="number" step="0.01" value={form.priceYearly} onChange={(e) => setField('priceYearly', Number(e.target.value))} className={inputClass} />
                    </FormField>
                </div>
                <FormField name="featuresJson" label="Features JSON">
                    <textarea id="featuresJson" value={form.featuresJson} onChange={(e) => setField('featuresJson', e.target.value)} rows={3} placeholder='{"sprintAnalytics":"basic","customWorkflows":false}' className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
                </FormField>
                <div className="flex justify-end gap-2 pt-2">
                    <button type="button" onClick={onClose} className="rounded-md border border-input px-4 py-2 text-sm font-medium text-foreground hover:bg-accent">Cancel</button>
                    <button type="submit" disabled={saving} className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
                        {saving ? 'Saving…' : isEdit ? 'Update Plan' : 'Create Plan'}
                    </button>
                </div>
            </form>
        </Modal>
    );
}
