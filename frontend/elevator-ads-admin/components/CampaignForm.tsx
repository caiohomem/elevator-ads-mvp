"use client";

import { useState } from "react";
import {
  createCampaign,
  updateCampaign,
  type CreateCampaignPayload,
  type UpdateCampaignPayload,
} from "@/lib/api/campaigns";
import { useTranslation } from "@/lib/i18n";
import type { ApiAdvertiser, ApiCampaign, CampaignStatus } from "@/lib/types";

type FormState = {
  advertiserId: string;
  name: string;
  status: CampaignStatus;
  startDate: string;
  endDate: string;
  dailyBudget: string;
  totalBudget: string;
  maxCpm: string;
};

type FieldErrors = Partial<Record<keyof FormState, string>>;

const statusOptions: CampaignStatus[] = ["Draft", "Scheduled", "Active", "Paused"];

function toFormState(initial?: ApiCampaign | null): FormState {
  return {
    advertiserId: initial?.advertiserId ?? "",
    name: initial?.name ?? "",
    status: normalizeStatus(initial?.status),
    startDate: toDateInput(initial?.startDate ?? null),
    endDate: toDateInput(initial?.endDate ?? null),
    dailyBudget: toBudgetInput(initial?.dailyBudget ?? null),
    totalBudget: toBudgetInput(initial?.totalBudget ?? null),
    maxCpm: toBudgetInput(initial?.maxCpm ?? null),
  };
}

function toDateInput(value: string | null) {
  return value ? value.slice(0, 10) : "";
}

function toBudgetInput(value: number | null) {
  return value !== null && value !== undefined ? String(value) : "";
}

function normalizeStatus(value: string | undefined): CampaignStatus {
  if (value === "Scheduled" || value === "Active" || value === "Paused") {
    return value;
  }

  return "Draft";
}

function validate(
  state: FormState,
  fieldRequired?: string,
  mustBePositive?: string,
  startBeforeEnd?: string,
): FieldErrors {
  const errors: FieldErrors = {};

  if (!state.advertiserId) {
    errors.advertiserId = fieldRequired;
  }

  if (!state.name.trim()) {
    errors.name = fieldRequired;
  }

  if (state.startDate && state.endDate) {
    const start = Date.parse(state.startDate);
    const end = Date.parse(state.endDate);

    if (Number.isFinite(start) && Number.isFinite(end) && start > end) {
      errors.endDate = startBeforeEnd;
    }
  }

  const budgetKeys: Array<keyof FormState> = ["dailyBudget", "totalBudget", "maxCpm"];

  for (const key of budgetKeys) {
    const value = state[key];
    if (value === "") {
      continue;
    }

    const parsed = Number(value);
    if (!Number.isFinite(parsed) || parsed < 0) {
      errors[key] = mustBePositive;
    }
  }

  return errors;
}

function parseBudget(value: string): number | null {
  if (value === "") {
    return null;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function toCreatePayload(state: FormState): CreateCampaignPayload {
  return {
    advertiserId: state.advertiserId,
    name: state.name.trim(),
    startDate: state.startDate ? new Date(state.startDate).toISOString() : null,
    endDate: state.endDate ? new Date(state.endDate).toISOString() : null,
    status: state.status,
    dailyBudget: parseBudget(state.dailyBudget),
    totalBudget: parseBudget(state.totalBudget),
    maxCpm: parseBudget(state.maxCpm),
  };
}

function toUpdatePayload(state: FormState): UpdateCampaignPayload {
  return {
    name: state.name.trim(),
    startDate: state.startDate ? new Date(state.startDate).toISOString() : null,
    endDate: state.endDate ? new Date(state.endDate).toISOString() : null,
    status: state.status,
    dailyBudget: parseBudget(state.dailyBudget),
    totalBudget: parseBudget(state.totalBudget),
    maxCpm: parseBudget(state.maxCpm),
  };
}

export function CampaignForm({
  initial,
  advertisers,
  onSuccess,
  onCancel,
}: {
  initial?: ApiCampaign | null;
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

    const validation = validate(state, forms.fieldRequired, forms.mustBePositive, forms.startBeforeEnd);
    setErrors(validation);
    if (Object.keys(validation).length > 0) {
      return;
    }

    setSubmitting(true);
    const result = isEdit && initial
      ? await updateCampaign(initial.id, toUpdatePayload(state))
      : await createCampaign(toCreatePayload(state));

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
        id="campaign-advertiser"
        label={forms.campaign.advertiserId}
        required
        error={errors.advertiserId}
      >
        <select
          id="campaign-advertiser"
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

      <FieldGroup id="campaign-name" label={forms.campaign.name} required error={errors.name}>
        <input
          id="campaign-name"
          type="text"
          value={state.name}
          onChange={(event) => updateField("name", event.target.value)}
          className={inputClass(Boolean(errors.name))}
          autoComplete="off"
        />
      </FieldGroup>

      <FieldGroup id="campaign-status" label={forms.campaign.status}>
        <select
          id="campaign-status"
          value={state.status}
          onChange={(event) => updateField("status", event.target.value as CampaignStatus)}
          className={inputClass(false)}
        >
          {statusOptions.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
      </FieldGroup>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FieldGroup id="campaign-start-date" label={forms.campaign.startDate}>
          <input
            id="campaign-start-date"
            type="date"
            value={state.startDate}
            onChange={(event) => updateField("startDate", event.target.value)}
            className={inputClass(false)}
          />
        </FieldGroup>

        <FieldGroup id="campaign-end-date" label={forms.campaign.endDate} error={errors.endDate}>
          <input
            id="campaign-end-date"
            type="date"
            value={state.endDate}
            onChange={(event) => updateField("endDate", event.target.value)}
            className={inputClass(Boolean(errors.endDate))}
          />
        </FieldGroup>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <FieldGroup
          id="campaign-daily-budget"
          label={forms.campaign.dailyBudget}
          error={errors.dailyBudget}
        >
          <input
            id="campaign-daily-budget"
            type="number"
            min={0}
            step="0.01"
            value={state.dailyBudget}
            onChange={(event) => updateField("dailyBudget", event.target.value)}
            className={inputClass(Boolean(errors.dailyBudget))}
          />
        </FieldGroup>

        <FieldGroup
          id="campaign-total-budget"
          label={forms.campaign.totalBudget}
          error={errors.totalBudget}
        >
          <input
            id="campaign-total-budget"
            type="number"
            min={0}
            step="0.01"
            value={state.totalBudget}
            onChange={(event) => updateField("totalBudget", event.target.value)}
            className={inputClass(Boolean(errors.totalBudget))}
          />
        </FieldGroup>

        <FieldGroup id="campaign-max-cpm" label={forms.campaign.maxCpm} error={errors.maxCpm}>
          <input
            id="campaign-max-cpm"
            type="number"
            min={0}
            step="0.01"
            value={state.maxCpm}
            onChange={(event) => updateField("maxCpm", event.target.value)}
            className={inputClass(Boolean(errors.maxCpm))}
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
