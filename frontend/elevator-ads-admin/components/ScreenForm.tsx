"use client";

import { useState } from "react";
import { createScreen, updateScreen, type CreateScreenPayload } from "@/lib/api/screens";
import { useTranslation } from "@/lib/i18n";
import type { ApiBuilding, ApiScreen } from "@/lib/types";

type FormState = {
  buildingId: string;
  name: string;
  externalCode: string;
  resolutionWidth: string;
  resolutionHeight: string;
  orientation: "Portrait" | "Landscape";
};

type FieldErrors = Partial<Record<keyof FormState, string>>;

function toFormState(initial?: ApiScreen | null): FormState {
  return {
    buildingId: initial?.buildingId ?? "",
    name: initial?.name ?? "",
    externalCode: initial?.externalCode ?? "",
    resolutionWidth: initial?.resolutionWidth !== undefined ? String(initial.resolutionWidth) : "1080",
    resolutionHeight: initial?.resolutionHeight !== undefined ? String(initial.resolutionHeight) : "1920",
    orientation: initial?.orientation === "Landscape" ? "Landscape" : "Portrait",
  };
}

function validate(state: FormState, fieldRequired?: string, mustBeGreaterThanZero?: string): FieldErrors {
  const errors: FieldErrors = {};

  if (!state.buildingId) {
    errors.buildingId = fieldRequired;
  }

  if (!state.name.trim()) {
    errors.name = fieldRequired;
  }

  const width = Number(state.resolutionWidth);
  if (!Number.isFinite(width) || width <= 0) {
    errors.resolutionWidth = mustBeGreaterThanZero;
  }

  const height = Number(state.resolutionHeight);
  if (!Number.isFinite(height) || height <= 0) {
    errors.resolutionHeight = mustBeGreaterThanZero;
  }

  return errors;
}

function toPayload(state: FormState, status: string): CreateScreenPayload {
  return {
    buildingId: state.buildingId,
    name: state.name.trim(),
    externalCode: state.externalCode.trim(),
    resolutionWidth: Number(state.resolutionWidth) || 0,
    resolutionHeight: Number(state.resolutionHeight) || 0,
    orientation: state.orientation,
    status,
  };
}

export function ScreenForm({
  initial,
  buildings,
  onSuccess,
  onCancel,
}: {
  initial?: ApiScreen | null;
  buildings: ApiBuilding[];
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
    const status = initial?.status ?? "Active";
    const payload = toPayload(state, status);
    const result = isEdit && initial ? await updateScreen(initial.id, payload) : await createScreen(payload);

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

      <FieldGroup id="screen-building" label={forms.screen.buildingId} required error={errors.buildingId}>
        <select
          id="screen-building"
          value={state.buildingId}
          onChange={(event) => updateField("buildingId", event.target.value)}
          className={inputClass(Boolean(errors.buildingId))}
        >
          <option value="">—</option>
          {buildings.map((building) => (
            <option key={building.id} value={building.id}>
              {building.name}
            </option>
          ))}
        </select>
      </FieldGroup>

      <FieldGroup id="screen-name" label={forms.screen.name} required error={errors.name}>
        <input
          id="screen-name"
          type="text"
          value={state.name}
          onChange={(event) => updateField("name", event.target.value)}
          className={inputClass(Boolean(errors.name))}
          autoComplete="off"
        />
      </FieldGroup>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FieldGroup
          id="screen-resolution-width"
          label={forms.screen.resolutionWidth}
          required
          error={errors.resolutionWidth}
        >
          <input
            id="screen-resolution-width"
            type="number"
            min={1}
            value={state.resolutionWidth}
            onChange={(event) => updateField("resolutionWidth", event.target.value)}
            className={inputClass(Boolean(errors.resolutionWidth))}
          />
        </FieldGroup>

        <FieldGroup
          id="screen-resolution-height"
          label={forms.screen.resolutionHeight}
          required
          error={errors.resolutionHeight}
        >
          <input
            id="screen-resolution-height"
            type="number"
            min={1}
            value={state.resolutionHeight}
            onChange={(event) => updateField("resolutionHeight", event.target.value)}
            className={inputClass(Boolean(errors.resolutionHeight))}
          />
        </FieldGroup>
      </div>

      <FieldGroup id="screen-orientation" label={forms.screen.orientation}>
        <select
          id="screen-orientation"
          value={state.orientation}
          onChange={(event) => updateField("orientation", event.target.value as FormState["orientation"])}
          className={inputClass(false)}
        >
          <option value="Portrait">Portrait</option>
          <option value="Landscape">Landscape</option>
        </select>
      </FieldGroup>

      <FieldGroup id="screen-external-code" label={forms.screen.externalCode}>
        <input
          id="screen-external-code"
          type="text"
          value={state.externalCode}
          onChange={(event) => updateField("externalCode", event.target.value)}
          className={inputClass(false)}
          autoComplete="off"
        />
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
