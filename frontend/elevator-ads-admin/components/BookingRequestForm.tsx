"use client";

import { useState } from "react";
import {
  createBookingRequest,
  updateBookingRequest,
  type CreateBookingRequestPayload,
  type UpdateBookingRequestPayload,
} from "@/lib/api/booking-requests";
import { useTranslation } from "@/lib/i18n";
import {
  DELIVERY_BUILDING_TYPES,
  DELIVERY_SCREEN_ORIENTATIONS,
  type ApiAdvertiser,
  type ApiBookingRequest,
  type DeliveryBuildingType,
  type DeliveryScreenOrientation,
} from "@/lib/types";

type FormMode = "create" | "edit";

type FormState = {
  advertiserId: string;
  name: string;
  dateFrom: string;
  dateTo: string;
  cities: string;
  buildingTypes: DeliveryBuildingType[];
  screenOrientations: DeliveryScreenOrientation[];
  creativeDurationSeconds: string;
  budget: string;
  campaignObjective: string;
  notes: string;
};

type FieldErrors = Partial<Record<keyof FormState, string>>;

function toFormState(initial?: ApiBookingRequest | null): FormState {
  return {
    advertiserId: initial?.advertiserId ?? "",
    name: initial?.name ?? "",
    dateFrom: toDateInput(initial?.dateFrom),
    dateTo: toDateInput(initial?.dateTo),
    cities: initial?.cities.join(", ") ?? "",
    buildingTypes: initial?.buildingTypes ?? [],
    screenOrientations: initial?.screenOrientations ?? [],
    creativeDurationSeconds: initial ? String(initial.creativeDurationSeconds) : "",
    budget: initial ? String(initial.budget) : "",
    campaignObjective: initial?.campaignObjective ?? "",
    notes: initial?.notes ?? "",
  };
}

function toDateInput(value?: string) {
  return value ? value.slice(0, 10) : "";
}

function parseStringList(value: string) {
  return value
    .split(",")
    .map((entry) => entry.trim())
    .filter(Boolean);
}

function validate(
  state: FormState,
  fieldRequired?: string,
  mustBePositive?: string,
  mustBeGreaterThanZero?: string,
  startBeforeEnd?: string,
): FieldErrors {
  const errors: FieldErrors = {};

  if (!state.advertiserId) {
    errors.advertiserId = fieldRequired;
  }

  if (!state.name.trim()) {
    errors.name = fieldRequired;
  }

  if (!state.dateFrom) {
    errors.dateFrom = fieldRequired;
  }

  if (!state.dateTo) {
    errors.dateTo = fieldRequired;
  }

  if (state.dateFrom && state.dateTo) {
    const from = Date.parse(state.dateFrom);
    const to = Date.parse(state.dateTo);
    if (Number.isFinite(from) && Number.isFinite(to) && from > to) {
      errors.dateTo = startBeforeEnd;
    }
  }

  const creativeDurationSeconds = Number(state.creativeDurationSeconds);
  if (!Number.isFinite(creativeDurationSeconds) || creativeDurationSeconds <= 0) {
    errors.creativeDurationSeconds = mustBeGreaterThanZero;
  }

  const budget = Number(state.budget);
  if (!Number.isFinite(budget) || budget < 0) {
    errors.budget = mustBePositive;
  }

  return errors;
}

function toCreatePayload(state: FormState): CreateBookingRequestPayload {
  return {
    advertiserId: state.advertiserId,
    name: state.name.trim(),
    dateFrom: new Date(state.dateFrom).toISOString(),
    dateTo: new Date(state.dateTo).toISOString(),
    cities: parseStringList(state.cities),
    buildingTypes: state.buildingTypes,
    screenOrientations: state.screenOrientations,
    creativeDurationSeconds: Number(state.creativeDurationSeconds),
    budget: Number(state.budget),
    campaignObjective: state.campaignObjective.trim(),
    notes: state.notes.trim(),
  };
}

function toUpdatePayload(state: FormState): UpdateBookingRequestPayload {
  const createPayload = toCreatePayload(state);
  return {
    name: createPayload.name,
    dateFrom: createPayload.dateFrom,
    dateTo: createPayload.dateTo,
    cities: createPayload.cities,
    buildingTypes: createPayload.buildingTypes,
    screenOrientations: createPayload.screenOrientations,
    creativeDurationSeconds: createPayload.creativeDurationSeconds,
    budget: createPayload.budget,
    campaignObjective: createPayload.campaignObjective,
    notes: createPayload.notes,
  };
}

export function BookingRequestForm({
  mode,
  initial,
  advertisers,
  onSuccess,
  onCancel,
}: {
  mode: FormMode;
  initial?: ApiBookingRequest | null;
  advertisers: ApiAdvertiser[];
  onSuccess: () => void;
  onCancel: () => void;
}) {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;
  const labels = forms.bookingRequest;

  const [state, setState] = useState<FormState>(() => toFormState(initial));
  const [errors, setErrors] = useState<FieldErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isEdit = mode === "edit" && Boolean(initial?.id);

  const updateField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setState((prev) => ({ ...prev, [key]: value }));
  };

  const toggleSelection = <T extends string>(
    key: "buildingTypes" | "screenOrientations",
    value: T,
  ) => {
    setState((prev) => {
      const current = prev[key] as T[];
      const next = current.includes(value)
        ? current.filter((item) => item !== value)
        : [...current, value];

      return { ...prev, [key]: next };
    });
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitError(null);

    const validation = validate(
      state,
      forms.fieldRequired,
      forms.mustBePositive,
      forms.mustBeGreaterThanZero,
      forms.startBeforeEnd,
    );
    setErrors(validation);
    if (Object.keys(validation).length > 0) {
      return;
    }

    setSubmitting(true);
    const result = isEdit && initial
      ? await updateBookingRequest(initial.id, toUpdatePayload(state))
      : await createBookingRequest(toCreatePayload(state));

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

      <FieldGroup id="booking-request-advertiser" label={labels.advertiser} required error={errors.advertiserId}>
        <select
          id="booking-request-advertiser"
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

      <FieldGroup id="booking-request-name" label={labels.name} required error={errors.name}>
        <input
          id="booking-request-name"
          type="text"
          value={state.name}
          onChange={(event) => updateField("name", event.target.value)}
          className={inputClass(Boolean(errors.name))}
          autoComplete="off"
        />
      </FieldGroup>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FieldGroup id="booking-request-date-from" label={labels.dateFrom} required error={errors.dateFrom}>
          <input
            id="booking-request-date-from"
            type="date"
            value={state.dateFrom}
            onChange={(event) => updateField("dateFrom", event.target.value)}
            className={inputClass(Boolean(errors.dateFrom))}
          />
        </FieldGroup>

        <FieldGroup id="booking-request-date-to" label={labels.dateTo} required error={errors.dateTo}>
          <input
            id="booking-request-date-to"
            type="date"
            value={state.dateTo}
            onChange={(event) => updateField("dateTo", event.target.value)}
            className={inputClass(Boolean(errors.dateTo))}
          />
        </FieldGroup>
      </div>

      <FieldGroup id="booking-request-cities" label={labels.cities}>
        <input
          id="booking-request-cities"
          type="text"
          value={state.cities}
          onChange={(event) => updateField("cities", event.target.value)}
          placeholder={labels.citiesHelp}
          className={inputClass(false)}
          autoComplete="off"
        />
      </FieldGroup>

      <MultiSelectGroup
        label={labels.buildingTypes}
        options={DELIVERY_BUILDING_TYPES}
        values={state.buildingTypes}
        onToggle={(value) => toggleSelection("buildingTypes", value)}
      />

      <MultiSelectGroup
        label={labels.screenOrientations}
        options={DELIVERY_SCREEN_ORIENTATIONS}
        values={state.screenOrientations}
        onToggle={(value) => toggleSelection("screenOrientations", value)}
      />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FieldGroup
          id="booking-request-creative-duration"
          label={labels.creativeDurationSeconds}
          required
          error={errors.creativeDurationSeconds}
        >
          <input
            id="booking-request-creative-duration"
            type="number"
            min={1}
            step="1"
            value={state.creativeDurationSeconds}
            onChange={(event) => updateField("creativeDurationSeconds", event.target.value)}
            className={inputClass(Boolean(errors.creativeDurationSeconds))}
          />
        </FieldGroup>

        <FieldGroup id="booking-request-budget" label={labels.budget} required error={errors.budget}>
          <input
            id="booking-request-budget"
            type="number"
            min={0}
            step="0.01"
            value={state.budget}
            onChange={(event) => updateField("budget", event.target.value)}
            className={inputClass(Boolean(errors.budget))}
          />
        </FieldGroup>
      </div>

      <FieldGroup id="booking-request-objective" label={labels.campaignObjective}>
        <input
          id="booking-request-objective"
          type="text"
          value={state.campaignObjective}
          onChange={(event) => updateField("campaignObjective", event.target.value)}
          className={inputClass(false)}
          autoComplete="off"
        />
      </FieldGroup>

      <FieldGroup id="booking-request-notes" label={labels.notes}>
        <textarea
          id="booking-request-notes"
          value={state.notes}
          onChange={(event) => updateField("notes", event.target.value)}
          className={`${inputClass(false)} min-h-28 resize-y`}
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

function MultiSelectGroup<T extends string>({
  label,
  options,
  values,
  onToggle,
}: {
  label: string;
  options: readonly T[];
  values: T[];
  onToggle: (value: T) => void;
}) {
  return (
    <div className="space-y-2">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">{label}</p>
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        {options.map((option) => {
          const checked = values.includes(option);
          return (
            <label
              key={option}
              className={`flex items-center gap-3 rounded-2xl border px-3.5 py-2.5 text-sm transition ${
                checked
                  ? "border-[var(--accent)] bg-[var(--accent)]/10 text-[var(--foreground)]"
                  : "border-[var(--panel-border)] bg-white/40 text-[var(--foreground)] dark:bg-white/[0.03]"
              }`}
            >
              <input
                type="checkbox"
                checked={checked}
                onChange={() => onToggle(option)}
                className="h-4 w-4 rounded border-[var(--panel-border)] text-[var(--accent)] focus:ring-[var(--accent)]"
              />
              <span>{option}</span>
            </label>
          );
        })}
      </div>
    </div>
  );
}

function inputClass(hasError: boolean) {
  return `w-full rounded-2xl border px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 ${
    hasError
      ? "border-rose-400 bg-rose-500/5"
      : "border-[var(--panel-border)] bg-white/60 dark:bg-white/5"
  }`;
}
