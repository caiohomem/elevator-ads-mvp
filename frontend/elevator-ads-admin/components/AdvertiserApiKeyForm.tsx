"use client";

import { useState } from "react";
import { createAdvertiserApiKey } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import type { CreateApiKeyResponse } from "@/lib/types";

const availableScopes = [
  "inventory:read",
  "forecast:create",
  "booking:create",
  "reports:read",
] as const;

type FormState = {
  name: string;
  scopes: string[];
  expiresAt: string;
};

type FieldErrors = {
  name?: string;
  scopes?: string;
};

export function AdvertiserApiKeyForm({
  advertiserId,
  onSuccess,
  onCancel,
}: {
  advertiserId: string;
  onSuccess: (result: CreateApiKeyResponse) => void;
  onCancel: () => void;
}) {
  const { dictionary } = useTranslation();
  const labels = dictionary.forms.advertiserApiKey;
  const [state, setState] = useState<FormState>({ name: "", scopes: [], expiresAt: "" });
  const [errors, setErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const toggleScope = (scope: string) => {
    setState((current) => ({
      ...current,
      scopes: current.scopes.includes(scope)
        ? current.scopes.filter((item) => item !== scope)
        : [...current.scopes, scope],
    }));
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitError(null);

    const nextErrors: FieldErrors = {};
    if (!state.name.trim()) {
      nextErrors.name = dictionary.forms.fieldRequired;
    }

    if (state.scopes.length === 0) {
      nextErrors.scopes = labels.scopeRequired;
    }

    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    setSubmitting(true);
    const result = await createAdvertiserApiKey(advertiserId, {
      name: state.name.trim(),
      scopes: state.scopes,
      expiresAt: state.expiresAt ? new Date(`${state.expiresAt}T00:00:00`).toISOString() : null,
    });
    setSubmitting(false);

    if (!result.ok) {
      setSubmitError(result.message || dictionary.forms.saveFailed);
      return;
    }

    onSuccess(result.data);
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

      <FieldGroup id="api-key-name" label={labels.name} error={errors.name} required>
        <input
          id="api-key-name"
          type="text"
          value={state.name}
          onChange={(event) => setState((current) => ({ ...current, name: event.target.value }))}
          className={inputClass(Boolean(errors.name))}
          autoComplete="off"
        />
      </FieldGroup>

      <FieldGroup id="api-key-scopes" label={labels.scopes} error={errors.scopes} required>
        <div className="grid grid-cols-1 gap-2 rounded-2xl border border-[var(--panel-border)] bg-[var(--panel-strong)]/70 p-3">
          {availableScopes.map((scope) => (
            <label
              key={scope}
              className="flex items-center gap-3 rounded-2xl px-3 py-2 text-sm text-[var(--foreground)] transition hover:bg-white/25 dark:hover:bg-white/[0.04]"
            >
              <input
                type="checkbox"
                checked={state.scopes.includes(scope)}
                onChange={() => toggleScope(scope)}
                className="h-4 w-4 rounded border-[var(--panel-border)] text-[var(--accent)] focus:ring-[var(--accent)]"
              />
              <div>
                <div className="font-medium">{labels.scopeLabels[scope]}</div>
                <div className="font-mono text-xs text-[var(--muted)]">{scope}</div>
              </div>
            </label>
          ))}
        </div>
      </FieldGroup>

      <FieldGroup id="api-key-expires-at" label={labels.expiresAt}>
        <input
          id="api-key-expires-at"
          type="date"
          value={state.expiresAt}
          onChange={(event) => setState((current) => ({ ...current, expiresAt: event.target.value }))}
          className={inputClass(false)}
        />
      </FieldGroup>

      <div className="rounded-2xl border border-amber-500/25 bg-amber-500/10 px-4 py-3 text-sm text-amber-800 dark:text-amber-200">
        {labels.shownOnceWarning}
      </div>

      <div className="flex flex-col-reverse gap-2 pt-2 sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={onCancel}
          className="rounded-2xl border border-[var(--panel-border)] px-4 py-2.5 text-sm font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
          disabled={submitting}
        >
          {dictionary.forms.cancel}
        </button>
        <button
          type="submit"
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-60"
          disabled={submitting}
        >
          {submitting ? dictionary.forms.saving : labels.create}
        </button>
      </div>
    </form>
  );
}

function FieldGroup({
  id,
  label,
  error,
  required,
  children,
}: {
  id: string;
  label: string;
  error?: string;
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-2">
      <label htmlFor={id} className="block text-sm font-medium text-[var(--foreground)]">
        {label}
        {required ? <span className="ml-1 text-rose-500">*</span> : null}
      </label>
      {children}
      {error ? <p className="text-sm text-rose-600 dark:text-rose-300">{error}</p> : null}
    </div>
  );
}

function inputClass(hasError: boolean) {
  return `w-full rounded-2xl border px-4 py-3 text-sm text-[var(--foreground)] outline-none transition ${
    hasError
      ? "border-rose-500/60 bg-rose-500/5 focus:border-rose-500"
      : "border-[var(--panel-border)] bg-[var(--panel-strong)]/70 focus:border-[var(--accent)]"
  }`;
}
