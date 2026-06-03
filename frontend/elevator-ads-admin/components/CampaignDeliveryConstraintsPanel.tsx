"use client";

import { useEffect, useMemo, useState } from "react";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import {
  getDeliveryConstraints,
  upsertDeliveryConstraints,
  type ApiDeliveryConstraints,
  type UpsertDeliveryConstraintsPayload,
} from "@/lib/api/campaigns";
import { useTranslation } from "@/lib/i18n";
import {
  DELIVERY_BUILDING_TYPES,
  DELIVERY_DAYS_OF_WEEK,
  DELIVERY_SCREEN_ORIENTATIONS,
  type DeliveryBuildingType,
  type DeliveryDayOfWeek,
  type DeliveryScreenOrientation,
} from "@/lib/types";

type LoadState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "ok" };

type FormState = {
  citiesText: string;
  buildingTypes: DeliveryBuildingType[];
  screenOrientations: DeliveryScreenOrientation[];
  daysOfWeek: DeliveryDayOfWeek[];
  startTime: string;
  endTime: string;
};

const EMPTY_FORM: FormState = {
  citiesText: "",
  buildingTypes: [],
  screenOrientations: [],
  daysOfWeek: [],
  startTime: "",
  endTime: "",
};

function toFormState(constraints: ApiDeliveryConstraints | null): FormState {
  if (!constraints) {
    return EMPTY_FORM;
  }

  return {
    citiesText: constraints.cities.join(", "),
    buildingTypes: filterDefined(
      constraints.buildingTypes,
      DELIVERY_BUILDING_TYPES,
    ) as DeliveryBuildingType[],
    screenOrientations: filterDefined(
      constraints.screenOrientations,
      DELIVERY_SCREEN_ORIENTATIONS,
    ) as DeliveryScreenOrientation[],
    daysOfWeek: filterDefined(
      constraints.daysOfWeek,
      DELIVERY_DAYS_OF_WEEK,
    ) as DeliveryDayOfWeek[],
    startTime: toTimeInput(constraints.startTime),
    endTime: toTimeInput(constraints.endTime),
  };
}

function toTimeInput(value: string | null) {
  if (!value) {
    return "";
  }

  return value.slice(0, 5);
}

function filterDefined<T extends string>(values: string[], allowed: readonly T[]): T[] {
  const set = new Set(allowed);
  const dedup = new Set<T>();
  const result: T[] = [];

  for (const value of values) {
    if (set.has(value as T) && !dedup.has(value as T)) {
      result.push(value as T);
      dedup.add(value as T);
    }
  }

  return result;
}

function parseCities(text: string) {
  return Array.from(
    new Set(
      text
        .split(",")
        .map((city) => city.trim())
        .filter((city) => city.length > 0),
    ),
  );
}

function buildPayload(state: FormState): UpsertDeliveryConstraintsPayload {
  return {
    cities: parseCities(state.citiesText),
    buildingTypes: state.buildingTypes,
    screenOrientations: state.screenOrientations,
    daysOfWeek: state.daysOfWeek,
    startTime: state.startTime ? state.startTime : null,
    endTime: state.endTime ? state.endTime : null,
  };
}

export function CampaignDeliveryConstraintsPanel({
  campaignId,
  onClose,
  onSaved,
}: {
  campaignId: string;
  onClose: () => void;
  onSaved?: () => void;
}) {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;
  const t = forms.deliveryConstraints;

  const [reloadKey, setReloadKey] = useState(0);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [timeError, setTimeError] = useState<string | null>(null);

  const handleSaved = () => {
    setSubmitError(null);
    setSuccess(true);
    setTimeError(null);
    onSaved?.();
  };

  const handleSubmitError = (message: string) => {
    setSubmitError(message);
    setSuccess(false);
  };

  return (
    <Modal open onClose={onClose} title={t.title}>
      {submitError ? (
        <div
          className="mb-4 rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-700 dark:text-rose-300"
          role="alert"
        >
          {submitError}
        </div>
      ) : null}

      {success ? (
        <div
          className="mb-4 rounded-2xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-700 dark:text-emerald-300"
          role="status"
        >
          {forms.saved}
        </div>
      ) : null}

      <DeliveryConstraintsForm
        key={reloadKey}
        campaignId={campaignId}
        onRetry={() => setReloadKey((value) => value + 1)}
        onSubmitError={handleSubmitError}
        onSubmitted={handleSaved}
        setTimeError={setTimeError}
        timeError={timeError}
      />
    </Modal>
  );
}

function DeliveryConstraintsForm({
  campaignId,
  onRetry,
  onSubmitError,
  onSubmitted,
  setTimeError,
  timeError,
}: {
  campaignId: string;
  onRetry: () => void;
  onSubmitError: (message: string) => void;
  onSubmitted: () => void;
  setTimeError: (message: string | null) => void;
  timeError: string | null;
}) {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;
  const t = forms.deliveryConstraints;

  const [loadState, setLoadState] = useState<LoadState>({ status: "loading" });
  const [formState, setFormState] = useState<FormState>(EMPTY_FORM);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    let active = true;

    getDeliveryConstraints(campaignId)
      .then((result) => {
        if (!active) {
          return;
        }

        if (result.ok) {
          setFormState(toFormState(result.data));
          setLoadState({ status: "ok" });
          return;
        }

        if (result.status === 404) {
          setFormState(EMPTY_FORM);
          setLoadState({ status: "ok" });
          return;
        }

        setLoadState({ status: "error", message: result.message });
      })
      .catch((error) => {
        if (!active) {
          return;
        }

        const message = error instanceof Error ? error.message : t.loadFailed;
        setLoadState({ status: "error", message });
      });

    return () => {
      active = false;
    };
  }, [campaignId, t.loadFailed]);

  const updateField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setFormState((prev) => ({ ...prev, [key]: value }));
  };

  const toggleValue = <T extends string>(current: T[], value: T) => {
    const next = current.includes(value)
      ? current.filter((item) => item !== value)
      : [...current, value];
    return next;
  };

  const timeRangeValid = useMemo(() => {
    if (!formState.startTime || !formState.endTime) {
      return true;
    }

    return formState.startTime < formState.endTime;
  }, [formState.startTime, formState.endTime]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    onSubmitError("");

    if (!timeRangeValid) {
      setTimeError(t.startBeforeEnd);
      return;
    }

    setTimeError(null);
    setSubmitting(true);

    const result = await upsertDeliveryConstraints(campaignId, buildPayload(formState));

    setSubmitting(false);

    if (!result.ok) {
      onSubmitError(result.message || forms.saveFailed);
      return;
    }

    setFormState(toFormState(result.data));
    onSubmitted();
  };

  if (loadState.status === "loading") {
    return <LoadingState />;
  }

  if (loadState.status === "error") {
    return (
      <div
        className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-700 dark:text-rose-300"
        role="alert"
      >
        <p className="font-semibold">{t.loadFailed}</p>
        <p className="mt-1 text-[var(--muted)]">{loadState.message}</p>
        <div className="mt-3 flex justify-end">
          <button
            type="button"
            onClick={onRetry}
            className="rounded-2xl bg-[var(--accent)] px-4 py-2 text-xs font-semibold text-white transition hover:bg-[var(--accent-strong)]"
          >
            {dictionary.common.retry}
          </button>
        </div>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-5" noValidate>
      <p className="text-xs text-[var(--muted)]">{t.emptyNote}</p>

      <FieldGroup id="dc-cities" label={t.cities} helpText={t.citiesHelp}>
        <input
          id="dc-cities"
          type="text"
          value={formState.citiesText}
          onChange={(event) => updateField("citiesText", event.target.value)}
          className={inputClass(false)}
          autoComplete="off"
          placeholder="Lisbon, Porto"
        />
      </FieldGroup>

      <FieldGroup id="dc-building-types" label={t.buildingTypes}>
        <CheckboxGroup
          name="buildingTypes"
          options={DELIVERY_BUILDING_TYPES}
          selected={formState.buildingTypes}
          onToggle={(value) =>
            updateField(
              "buildingTypes",
              toggleValue<DeliveryBuildingType>(formState.buildingTypes, value),
            )
          }
        />
      </FieldGroup>

      <FieldGroup id="dc-screen-orientations" label={t.screenOrientations}>
        <CheckboxGroup
          name="screenOrientations"
          options={DELIVERY_SCREEN_ORIENTATIONS}
          selected={formState.screenOrientations}
          onToggle={(value) =>
            updateField(
              "screenOrientations",
              toggleValue<DeliveryScreenOrientation>(formState.screenOrientations, value),
            )
          }
        />
      </FieldGroup>

      <FieldGroup id="dc-days-of-week" label={t.daysOfWeek}>
        <CheckboxGroup
          name="daysOfWeek"
          options={DELIVERY_DAYS_OF_WEEK}
          selected={formState.daysOfWeek}
          onToggle={(value) =>
            updateField(
              "daysOfWeek",
              toggleValue<DeliveryDayOfWeek>(formState.daysOfWeek, value),
            )
          }
        />
      </FieldGroup>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FieldGroup id="dc-start-time" label={t.startTime}>
          <input
            id="dc-start-time"
            type="time"
            value={formState.startTime}
            onChange={(event) => {
              updateField("startTime", event.target.value);
              setTimeError(null);
            }}
            className={inputClass(Boolean(timeError))}
          />
        </FieldGroup>

        <FieldGroup id="dc-end-time" label={t.endTime} error={timeError ?? undefined}>
          <input
            id="dc-end-time"
            type="time"
            value={formState.endTime}
            onChange={(event) => {
              updateField("endTime", event.target.value);
              setTimeError(null);
            }}
            className={inputClass(Boolean(timeError))}
          />
        </FieldGroup>
      </div>

      <p className="text-xs text-[var(--muted)]">{t.timeHelp}</p>

      <div className="flex flex-col-reverse gap-2 pt-2 sm:flex-row sm:justify-end">
        <button
          type="submit"
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-60"
          disabled={submitting}
        >
          {submitting ? forms.saving : t.save}
        </button>
      </div>
    </form>
  );
}

function FieldGroup({
  id,
  label,
  helpText,
  error,
  children,
}: {
  id: string;
  label: string;
  helpText?: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">
        {label}
      </label>
      {children}
      {error ? <p className="text-xs font-medium text-rose-600 dark:text-rose-300">{error}</p> : null}
      {helpText ? <p className="text-xs text-[var(--muted)]">{helpText}</p> : null}
    </div>
  );
}

function CheckboxGroup<T extends string>({
  name,
  options,
  selected,
  onToggle,
}: {
  name: string;
  options: readonly T[];
  selected: T[];
  onToggle: (value: T) => void;
}) {
  return (
    <div className="flex flex-wrap gap-2">
      {options.map((option) => {
        const checked = selected.includes(option);
        const id = `dc-${name}-${option}`;
        return (
          <label
            key={option}
            htmlFor={id}
            className={`flex cursor-pointer items-center gap-2 rounded-full border px-3 py-1.5 text-xs font-semibold transition ${
              checked
                ? "border-[var(--accent)] bg-[var(--accent)]/10 text-[var(--foreground)]"
                : "border-[var(--panel-border)] text-[var(--muted)] hover:bg-white/30 dark:hover:bg-white/5"
            }`}
          >
            <input
              id={id}
              type="checkbox"
              name={name}
              value={option}
              checked={checked}
              onChange={() => onToggle(option)}
              className="h-3.5 w-3.5 accent-[var(--accent)]"
            />
            <span>{option}</span>
          </label>
        );
      })}
    </div>
  );
}

function inputClass(hasError: boolean) {
  return `w-full rounded-2xl border bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition placeholder:text-[var(--muted)] focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5 ${
    hasError ? "border-rose-500/60" : "border-[var(--panel-border)]"
  }`;
}
