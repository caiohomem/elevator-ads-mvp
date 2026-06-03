"use client";

import { useState } from "react";
import { createBuilding, updateBuilding, type CreateBuildingPayload } from "@/lib/api/buildings";
import { useTranslation } from "@/lib/i18n";
import type { ApiBuilding, BuildingType } from "@/lib/types";

type FormState = {
  name: string;
  address: string;
  city: string;
  country: string;
  postalCode: string;
  buildingType: BuildingType;
  estimatedDailyAudience: string;
};

type FieldErrors = Partial<Record<keyof FormState, string>>;

const buildingTypeOptions: BuildingType[] = ["Residential", "Commercial", "MixedUse", "Hospitality"];

function toFormState(initial?: ApiBuilding | null): FormState {
  return {
    name: initial?.name ?? "",
    address: initial?.address ?? "",
    city: initial?.city ?? "",
    country: initial?.country ?? "",
    postalCode: initial?.postalCode ?? "",
    buildingType: normalizeBuildingType(initial?.buildingType),
    estimatedDailyAudience:
      initial?.estimatedDailyAudience !== undefined ? String(initial.estimatedDailyAudience) : "0",
  };
}

function normalizeBuildingType(value: string | undefined): BuildingType {
  if (
    value === "Residential" ||
    value === "Commercial" ||
    value === "MixedUse" ||
    value === "Hospitality"
  ) {
    return value;
  }

  if (value === "Corporate") {
    return "Commercial";
  }

  return "MixedUse";
}

function validate(state: FormState, fieldRequired?: string, mustBePositive?: string): FieldErrors {
  const errors: FieldErrors = {};

  if (!state.name.trim()) {
    errors.name = fieldRequired;
  }

  if (!state.city.trim()) {
    errors.city = fieldRequired;
  }

  if (!state.country.trim()) {
    errors.country = fieldRequired;
  }

  const audience = Number(state.estimatedDailyAudience);
  if (!Number.isFinite(audience) || audience < 0) {
    errors.estimatedDailyAudience = mustBePositive;
  }

  return errors;
}

function toPayload(state: FormState): CreateBuildingPayload {
  return {
    name: state.name.trim(),
    address: state.address.trim(),
    city: state.city.trim(),
    country: state.country.trim(),
    postalCode: state.postalCode.trim(),
    buildingType: state.buildingType,
    estimatedDailyAudience: Number(state.estimatedDailyAudience) || 0,
  };
}

export function BuildingForm({
  initial,
  onSuccess,
  onCancel,
}: {
  initial?: ApiBuilding | null;
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

    const validation = validate(state, forms.fieldRequired, forms.mustBePositive);
    setErrors(validation);
    if (Object.keys(validation).length > 0) {
      return;
    }

    setSubmitting(true);
    const payload = toPayload(state);
    const result = isEdit && initial ? await updateBuilding(initial.id, payload) : await createBuilding(payload);

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

      <FieldGroup id="building-name" label={forms.building.name} required error={errors.name}>
        <input
          id="building-name"
          type="text"
          value={state.name}
          onChange={(event) => updateField("name", event.target.value)}
          className={inputClass(Boolean(errors.name))}
          autoComplete="off"
        />
      </FieldGroup>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FieldGroup id="building-city" label={forms.building.city} required error={errors.city}>
          <input
            id="building-city"
            type="text"
            value={state.city}
            onChange={(event) => updateField("city", event.target.value)}
            className={inputClass(Boolean(errors.city))}
            autoComplete="off"
          />
        </FieldGroup>

        <FieldGroup id="building-country" label={forms.building.country} required error={errors.country}>
          <input
            id="building-country"
            type="text"
            value={state.country}
            onChange={(event) => updateField("country", event.target.value)}
            className={inputClass(Boolean(errors.country))}
            autoComplete="off"
          />
        </FieldGroup>
      </div>

      <FieldGroup id="building-type" label={forms.building.type}>
        <select
          id="building-type"
          value={state.buildingType}
          onChange={(event) => updateField("buildingType", event.target.value as BuildingType)}
          className={inputClass(false)}
        >
          {buildingTypeOptions.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
      </FieldGroup>

      <FieldGroup
        id="building-audience"
        label={forms.building.audience}
        required
        error={errors.estimatedDailyAudience}
      >
        <input
          id="building-audience"
          type="number"
          min={0}
          value={state.estimatedDailyAudience}
          onChange={(event) => updateField("estimatedDailyAudience", event.target.value)}
          className={inputClass(Boolean(errors.estimatedDailyAudience))}
        />
      </FieldGroup>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FieldGroup id="building-address" label={forms.building.address}>
          <input
            id="building-address"
            type="text"
            value={state.address}
            onChange={(event) => updateField("address", event.target.value)}
            className={inputClass(false)}
            autoComplete="off"
          />
        </FieldGroup>

        <FieldGroup id="building-postal-code" label={forms.building.postalCode}>
          <input
            id="building-postal-code"
            type="text"
            value={state.postalCode}
            onChange={(event) => updateField("postalCode", event.target.value)}
            className={inputClass(false)}
            autoComplete="off"
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
