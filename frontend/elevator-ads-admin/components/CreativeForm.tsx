"use client";

import { useState } from "react";
import {
  createCreative,
  updateCreative,
  type CreateCreativePayload,
  type UpdateCreativePayload,
} from "@/lib/api/creatives";
import { useTranslation } from "@/lib/i18n";
import type { ApiAdvertiser, ApiCreative } from "@/lib/types";

type MediaTypeOption = "Image" | "Video";

type FormState = {
  advertiserId: string;
  name: string;
  mediaUrl: string;
  mediaType: MediaTypeOption;
  durationSeconds: string;
};

type FieldErrors = Partial<Record<keyof FormState, string>>;

const mediaTypeOptions: MediaTypeOption[] = ["Image", "Video"];

function toFormState(initial?: ApiCreative | null): FormState {
  return {
    advertiserId: initial?.advertiserId ?? "",
    name: initial?.name ?? "",
    mediaUrl: initial?.mediaUrl ?? "",
    mediaType: initial?.mediaType === "Video" ? "Video" : "Image",
    durationSeconds: initial?.durationSeconds !== undefined ? String(initial.durationSeconds) : "10",
  };
}

function validate(
  state: FormState,
  fieldRequired?: string,
  mustBeGreaterThanZero?: string,
): FieldErrors {
  const errors: FieldErrors = {};

  if (!state.advertiserId) {
    errors.advertiserId = fieldRequired;
  }

  if (!state.name.trim()) {
    errors.name = fieldRequired;
  }

  if (!state.mediaUrl.trim()) {
    errors.mediaUrl = fieldRequired;
  }

  const duration = Number(state.durationSeconds);
  if (!Number.isFinite(duration) || duration <= 0) {
    errors.durationSeconds = mustBeGreaterThanZero;
  }

  return errors;
}

function toCreatePayload(state: FormState): CreateCreativePayload {
  return {
    advertiserId: state.advertiserId,
    name: state.name.trim(),
    mediaUrl: state.mediaUrl.trim(),
    mediaType: state.mediaType,
    durationSeconds: Number(state.durationSeconds) || 0,
  };
}

function toUpdatePayload(state: FormState): UpdateCreativePayload {
  return {
    name: state.name.trim(),
    mediaUrl: state.mediaUrl.trim(),
    mediaType: state.mediaType,
    durationSeconds: Number(state.durationSeconds) || 0,
  };
}

export function CreativeForm({
  initial,
  advertisers,
  onSuccess,
  onCancel,
}: {
  initial?: ApiCreative | null;
  advertisers: ApiAdvertiser[];
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

    const validation = validate(state, forms.fieldRequired, forms.mustBeGreaterThanZero);
    setErrors(validation);
    if (Object.keys(validation).length > 0) {
      return;
    }

    setSubmitting(true);
    const result = isEdit && initial
      ? await updateCreative(initial.id, toUpdatePayload(state))
      : await createCreative(toCreatePayload(state));

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

      <FieldGroup
        id="creative-advertiser"
        label={forms.creative.advertiserId}
        required
        error={errors.advertiserId}
      >
        <select
          id="creative-advertiser"
          value={state.advertiserId}
          onChange={(event) => updateField("advertiserId", event.target.value)}
          className={inputClass(Boolean(errors.advertiserId))}
          disabled={isEdit}
        >
          <option value="">—</option>
          {advertisers.map((advertiser) => (
            <option key={advertiser.id} value={advertiser.id}>
              {advertiser.name}
            </option>
          ))}
        </select>
      </FieldGroup>

      <FieldGroup id="creative-name" label={forms.creative.name} required error={errors.name}>
        <input
          id="creative-name"
          type="text"
          value={state.name}
          onChange={(event) => updateField("name", event.target.value)}
          className={inputClass(Boolean(errors.name))}
          autoComplete="off"
        />
      </FieldGroup>

      <FieldGroup id="creative-media-url" label={forms.creative.mediaUrl} required error={errors.mediaUrl}>
        <input
          id="creative-media-url"
          type="url"
          value={state.mediaUrl}
          onChange={(event) => updateField("mediaUrl", event.target.value)}
          className={inputClass(Boolean(errors.mediaUrl))}
          autoComplete="off"
          placeholder="https://"
        />
      </FieldGroup>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FieldGroup id="creative-media-type" label={forms.creative.mediaType}>
          <select
            id="creative-media-type"
            value={state.mediaType}
            onChange={(event) => updateField("mediaType", event.target.value as MediaTypeOption)}
            className={inputClass(false)}
          >
            {mediaTypeOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </FieldGroup>

        <FieldGroup
          id="creative-duration"
          label={forms.creative.durationSeconds}
          required
          error={errors.durationSeconds}
        >
          <input
            id="creative-duration"
            type="number"
            min={1}
            value={state.durationSeconds}
            onChange={(event) => updateField("durationSeconds", event.target.value)}
            className={inputClass(Boolean(errors.durationSeconds))}
          />
        </FieldGroup>
      </div>

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
