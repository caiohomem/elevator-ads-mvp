"use client";

import { useState } from "react";
import {
  createInventoryPackage,
  updateInventoryPackage,
  type InventoryPackagePayload,
} from "@/lib/api/inventory-packages";
import { useTranslation } from "@/lib/i18n";
import {
  DELIVERY_BUILDING_TYPES,
  DELIVERY_SCREEN_ORIENTATIONS,
  type ApiBuilding,
  type ApiInventoryPackage,
  type ApiScreen,
  type DeliveryBuildingType,
  type DeliveryScreenOrientation,
  type InventoryPackageStatus,
} from "@/lib/types";

type FormMode = "create" | "edit";

type FormState = {
  name: string;
  description: string;
  cities: string;
  buildingTypes: DeliveryBuildingType[];
  screenOrientations: DeliveryScreenOrientation[];
  buildingIds: string[];
  screenIds: string[];
  baseCpm: string;
  status: InventoryPackageStatus;
};

type FieldErrors = Partial<Record<keyof FormState, string>>;

function toFormState(initial?: ApiInventoryPackage | null): FormState {
  return {
    name: initial?.name ?? "",
    description: initial?.description ?? "",
    cities: initial?.cities.join(", ") ?? "",
    buildingTypes: initial?.buildingTypes ?? [],
    screenOrientations: initial?.screenOrientations ?? [],
    buildingIds: initial?.buildingIds ?? [],
    screenIds: initial?.screenIds ?? [],
    baseCpm: initial ? String(initial.baseCpm) : "0",
    status: initial?.status ?? "Active",
  };
}

function parseStringList(value: string) {
  return value
    .split(",")
    .map((entry) => entry.trim())
    .filter(Boolean);
}

function validate(state: FormState, fieldRequired?: string, mustBePositive?: string): FieldErrors {
  const errors: FieldErrors = {};

  if (!state.name.trim()) {
    errors.name = fieldRequired;
  }

  const baseCpm = Number(state.baseCpm);
  if (!Number.isFinite(baseCpm) || baseCpm < 0) {
    errors.baseCpm = mustBePositive;
  }

  return errors;
}

function toPayload(state: FormState): InventoryPackagePayload {
  return {
    name: state.name.trim(),
    description: state.description.trim(),
    cities: parseStringList(state.cities),
    buildingTypes: state.buildingTypes,
    screenOrientations: state.screenOrientations,
    buildingIds: state.buildingIds,
    screenIds: state.screenIds,
    baseCpm: Number(state.baseCpm),
    status: state.status,
  };
}

export function InventoryPackageForm({
  mode,
  initial,
  buildings,
  screens,
  onSuccess,
  onCancel,
}: {
  mode: FormMode;
  initial?: ApiInventoryPackage | null;
  buildings: ApiBuilding[];
  screens: ApiScreen[];
  onSuccess: () => void;
  onCancel: () => void;
}) {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;
  const labels = forms.inventoryPackage;

  const [state, setState] = useState<FormState>(() => toFormState(initial));
  const [errors, setErrors] = useState<FieldErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isEdit = mode === "edit" && Boolean(initial?.id);

  const updateField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setState((prev) => ({ ...prev, [key]: value }));
  };

  const toggleSelection = <T extends string>(
    key: "buildingTypes" | "screenOrientations" | "buildingIds" | "screenIds",
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

    const validation = validate(state, forms.fieldRequired, forms.mustBePositive);
    setErrors(validation);
    if (Object.keys(validation).length > 0) {
      return;
    }

    setSubmitting(true);
    const payload = toPayload(state);
    const result = isEdit && initial
      ? await updateInventoryPackage(initial.id, payload)
      : await createInventoryPackage(payload);

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

      <FieldGroup id="inventory-package-name" label={labels.name} required error={errors.name}>
        <input
          id="inventory-package-name"
          type="text"
          value={state.name}
          onChange={(event) => updateField("name", event.target.value)}
          className={inputClass(Boolean(errors.name))}
          autoComplete="off"
        />
      </FieldGroup>

      <FieldGroup id="inventory-package-description" label={labels.description}>
        <textarea
          id="inventory-package-description"
          value={state.description}
          onChange={(event) => updateField("description", event.target.value)}
          className={`${inputClass(false)} min-h-24 resize-y`}
        />
      </FieldGroup>

      <FieldGroup id="inventory-package-cities" label={labels.cities}>
        <input
          id="inventory-package-cities"
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

      <ReferenceSelectGroup
        label={labels.buildings}
        items={buildings.map((building) => ({
          id: building.id,
          label: `${building.name} · ${building.city}`,
        }))}
        values={state.buildingIds}
        onToggle={(value) => toggleSelection("buildingIds", value)}
      />

      <ReferenceSelectGroup
        label={labels.screens}
        items={screens.map((screen) => ({
          id: screen.id,
          label: `${screen.name} · ${screen.orientation}`,
        }))}
        values={state.screenIds}
        onToggle={(value) => toggleSelection("screenIds", value)}
      />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FieldGroup id="inventory-package-base-cpm" label={labels.baseCpm} required error={errors.baseCpm}>
          <input
            id="inventory-package-base-cpm"
            type="number"
            min={0}
            step="0.01"
            value={state.baseCpm}
            onChange={(event) => updateField("baseCpm", event.target.value)}
            className={inputClass(Boolean(errors.baseCpm))}
          />
        </FieldGroup>

        <FieldGroup id="inventory-package-status" label={labels.status}>
          <select
            id="inventory-package-status"
            value={state.status}
            onChange={(event) => updateField("status", event.target.value as InventoryPackageStatus)}
            className={inputClass(false)}
          >
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
          </select>
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

function ReferenceSelectGroup({
  label,
  items,
  values,
  onToggle,
}: {
  label: string;
  items: { id: string; label: string }[];
  values: string[];
  onToggle: (value: string) => void;
}) {
  return (
    <div className="space-y-2">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">{label}</p>
      <div className="max-h-52 space-y-2 overflow-y-auto rounded-2xl border border-[var(--panel-border)] bg-white/50 p-3 dark:bg-white/[0.03]">
        {items.length ? (
          items.map((item) => {
            const checked = values.includes(item.id);
            return (
              <label key={item.id} className="flex items-start gap-3 text-sm text-[var(--foreground)]">
                <input
                  type="checkbox"
                  checked={checked}
                  onChange={() => onToggle(item.id)}
                  className="mt-0.5 h-4 w-4 rounded border-[var(--panel-border)] text-[var(--accent)] focus:ring-[var(--accent)]"
                />
                <span>{item.label}</span>
              </label>
            );
          })
        ) : (
          <p className="text-sm text-[var(--muted)]">—</p>
        )}
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
