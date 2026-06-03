"use client";

import { useState } from "react";
import {
  createAdvertiser,
  updateAdvertiser,
  type CreateAdvertiserPayload,
} from "@/lib/api/advertisers";
import { useTranslation } from "@/lib/i18n";
import type { ApiAdvertiser, EntityStatus } from "@/lib/types";

type FormState = {
  name: string;
  legalName: string;
  taxId: string;
  contactName: string;
  contactEmail: string;
  phone: string;
  status: EntityStatus;
};

type FieldErrors = Partial<Record<keyof FormState, string>>;

const statusOptions: EntityStatus[] = ["Active", "Inactive"];
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function toFormState(initial?: ApiAdvertiser | null): FormState {
  return {
    name: initial?.name ?? "",
    legalName: initial?.legalName ?? "",
    taxId: initial?.taxId ?? "",
    contactName: initial?.contactName ?? "",
    contactEmail: initial?.contactEmail ?? "",
    phone: initial?.phone ?? "",
    status: normalizeStatus(initial?.status),
  };
}

function normalizeStatus(value: string | undefined): EntityStatus {
  return value === "Inactive" ? "Inactive" : "Active";
}

function validate(state: FormState, fieldRequired?: string, invalidEmail?: string): FieldErrors {
  const errors: FieldErrors = {};

  if (!state.name.trim()) {
    errors.name = fieldRequired;
  }

  const email = state.contactEmail.trim();
  if (email && !emailPattern.test(email)) {
    errors.contactEmail = invalidEmail;
  }

  return errors;
}

function toPayload(state: FormState): CreateAdvertiserPayload {
  return {
    name: state.name.trim(),
    legalName: state.legalName.trim(),
    taxId: state.taxId.trim(),
    contactName: state.contactName.trim(),
    contactEmail: state.contactEmail.trim(),
    phone: state.phone.trim(),
    status: state.status,
  };
}

export function AdvertiserForm({
  initial,
  onSuccess,
  onCancel,
}: {
  initial?: ApiAdvertiser | null;
  onSuccess: () => void;
  onCancel: () => void;
}) {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;

  const [state, setState] = useState<FormState>(() => toFormState(initial));
  const [errors, setErrors] = useState<FieldErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isEdit = Boolean(initial?.id);

  const updateField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setState((prev) => ({ ...prev, [key]: value }));
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitError(null);

    const validation = validate(state, forms.fieldRequired, forms.invalidEmail);
    setErrors(validation);
    if (Object.keys(validation).length > 0) {
      return;
    }

    setSubmitting(true);
    const payload = toPayload(state);
    const result = isEdit && initial ? await updateAdvertiser(initial.id, payload) : await createAdvertiser(payload);

    if (!result.ok) {
      setSubmitError(result.message || forms.saveFailed);
      setSubmitting(false);
      return;
    }

    setSubmitting(false);
    onSuccess();
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4" noValidate>
      {submitError ? (
        <div
          className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-700 dark:text-rose-300"
          role="alert"
        >
          {submitError}
        </div>
      ) : null}

      <FieldGroup id="advertiser-name" label={forms.advertiser.name} required error={errors.name}>
        <input
          id="advertiser-name"
          type="text"
          value={state.name}
          onChange={(event) => updateField("name", event.target.value)}
          className={inputClass(Boolean(errors.name))}
          autoComplete="off"
        />
      </FieldGroup>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FieldGroup id="advertiser-legal-name" label={forms.advertiser.legalName}>
          <input
            id="advertiser-legal-name"
            type="text"
            value={state.legalName}
            onChange={(event) => updateField("legalName", event.target.value)}
            className={inputClass(false)}
            autoComplete="off"
          />
        </FieldGroup>

        <FieldGroup id="advertiser-tax-id" label={forms.advertiser.taxId}>
          <input
            id="advertiser-tax-id"
            type="text"
            value={state.taxId}
            onChange={(event) => updateField("taxId", event.target.value)}
            className={inputClass(false)}
            autoComplete="off"
          />
        </FieldGroup>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FieldGroup id="advertiser-contact-name" label={forms.advertiser.contactName}>
          <input
            id="advertiser-contact-name"
            type="text"
            value={state.contactName}
            onChange={(event) => updateField("contactName", event.target.value)}
            className={inputClass(false)}
            autoComplete="off"
          />
        </FieldGroup>

        <FieldGroup
          id="advertiser-contact-email"
          label={forms.advertiser.contactEmail}
          error={errors.contactEmail}
        >
          <input
            id="advertiser-contact-email"
            type="email"
            value={state.contactEmail}
            onChange={(event) => updateField("contactEmail", event.target.value)}
            className={inputClass(Boolean(errors.contactEmail))}
            autoComplete="off"
          />
        </FieldGroup>
      </div>

      <FieldGroup id="advertiser-phone" label={forms.advertiser.phone}>
        <input
          id="advertiser-phone"
          type="text"
          value={state.phone}
          onChange={(event) => updateField("phone", event.target.value)}
          className={inputClass(false)}
          autoComplete="off"
        />
      </FieldGroup>

      <FieldGroup id="advertiser-status" label={forms.advertiser.status}>
        <select
          id="advertiser-status"
          value={state.status}
          onChange={(event) => updateField("status", event.target.value as EntityStatus)}
          className={inputClass(false)}
        >
          {statusOptions.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
      </FieldGroup>

      <div className="flex flex-col-reverse gap-2 pt-2 sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={onCancel}
          className="rounded-2xl border border-[var(--panel-border)] px-4 py-2.5 text-sm font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
          disabled={submitting}
        >
          {forms.cancel}
        </button>
        <button
          type="submit"
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-60"
          disabled={submitting}
        >
          {submitting ? forms.saving : forms.save}
        </button>
      </div>
    </form>
  );
}

function FieldGroup({
  id,
  label,
  required,
  error,
  children,
}: {
  id: string;
  label: string;
  required?: boolean;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">
        {label}
        {required ? <span className="ml-1 text-[var(--accent)]">*</span> : null}
      </label>
      {children}
      {error ? <p className="text-xs font-medium text-rose-600 dark:text-rose-300">{error}</p> : null}
    </div>
  );
}

function inputClass(hasError: boolean) {
  return `w-full rounded-2xl border bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition placeholder:text-[var(--muted)] focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5 ${
    hasError ? "border-rose-500/60" : "border-[var(--panel-border)]"
  }`;
}
