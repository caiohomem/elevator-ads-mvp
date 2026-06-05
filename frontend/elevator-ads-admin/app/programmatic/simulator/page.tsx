"use client";

import { useMemo, useState } from "react";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { SummaryCard } from "@/components/SummaryCard";
import { getAdvertisersList, runSimulatorForecast } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { useTranslation } from "@/lib/i18n";
import {
  DELIVERY_BUILDING_TYPES,
  DELIVERY_SCREEN_ORIENTATIONS,
  type ApiAdvertiser,
  type DeliveryBuildingType,
  type DeliveryScreenOrientation,
  type SimulatorForecastRequest,
  type SimulatorForecastResponse,
} from "@/lib/types";

type ResultState =
  | { status: "idle" }
  | { status: "loading"; payload: SimulatorForecastRequest }
  | { status: "error"; message: string; payload: SimulatorForecastRequest }
  | { status: "result"; data: SimulatorForecastResponse; payload: SimulatorForecastRequest };

function toIsoDate(value: Date): string {
  return value.toISOString().slice(0, 10);
}

function defaultDateRange() {
  const from = new Date();
  const to = new Date();
  to.setDate(from.getDate() + 7);
  return {
    dateFrom: toIsoDate(from),
    dateTo: toIsoDate(to),
  };
}

function parseCommaSeparated(value: string) {
  const parsed = value
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);

  return parsed.length > 0 ? parsed : null;
}

function toggleValue<T extends string>(values: T[], value: T) {
  return values.includes(value) ? values.filter((item) => item !== value) : [...values, value];
}

function formatCapacity(value: number) {
  const percent = value <= 1 ? value * 100 : value;
  return `${percent.toFixed(percent % 1 === 0 ? 0 : 1)}%`;
}

export default function ProgrammaticSimulatorPage() {
  const { dictionary } = useTranslation();
  const labels = dictionary.simulator;
  const forms = dictionary.forms;
  const advertisersState = useApiData(getAdvertisersList);

  const initialDates = useMemo(() => defaultDateRange(), []);
  const [advertiserId, setAdvertiserId] = useState("");
  const [dateFrom, setDateFrom] = useState(initialDates.dateFrom);
  const [dateTo, setDateTo] = useState(initialDates.dateTo);
  const [cities, setCities] = useState("");
  const [buildingTypes, setBuildingTypes] = useState<DeliveryBuildingType[]>([]);
  const [screenOrientations, setScreenOrientations] = useState<DeliveryScreenOrientation[]>([]);
  const [creativeDurationSeconds, setCreativeDurationSeconds] = useState("15");
  const [budget, setBudget] = useState("");
  const [campaignObjective, setCampaignObjective] = useState("");
  const [notes, setNotes] = useState("");
  const [localError, setLocalError] = useState<string | null>(null);
  const [resultState, setResultState] = useState<ResultState>({ status: "idle" });

  const advertiserOptions: ApiAdvertiser[] = advertisersState.status === "ok" ? advertisersState.data : [];

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setLocalError(null);

    const duration = Number(creativeDurationSeconds);
    if (dateFrom > dateTo) {
      setLocalError(forms.startBeforeEnd);
      return;
    }

    if (!Number.isFinite(duration) || duration < 1) {
      setLocalError(forms.mustBeGreaterThanZero);
      return;
    }

    const parsedBudget = budget.trim() ? Number(budget) : null;
    if (parsedBudget !== null && (!Number.isFinite(parsedBudget) || parsedBudget < 0)) {
      setLocalError(forms.mustBePositive);
      return;
    }

    const payload: SimulatorForecastRequest = {
      advertiserId: advertiserId.trim() || null,
      dateFrom,
      dateTo,
      cities: parseCommaSeparated(cities),
      buildingTypes: buildingTypes.length > 0 ? buildingTypes : null,
      screenOrientations: screenOrientations.length > 0 ? screenOrientations : null,
      creativeDurationSeconds: duration,
      budget: parsedBudget,
      campaignObjective: campaignObjective.trim() || null,
      notes: notes.trim() || null,
    };

    setResultState({ status: "loading", payload });
    const result = await runSimulatorForecast(payload);

    if (!result.ok) {
      setResultState({ status: "error", message: result.message, payload });
      return;
    }

    setResultState({ status: "result", data: result.data, payload });
  };

  const retryLastRun = async () => {
    if (resultState.status !== "error") {
      return;
    }

    setResultState({ status: "loading", payload: resultState.payload });
    const result = await runSimulatorForecast(resultState.payload);

    if (!result.ok) {
      setResultState({ status: "error", message: result.message, payload: resultState.payload });
      return;
    }

    setResultState({ status: "result", data: result.data, payload: resultState.payload });
  };

  return (
    <ClientPageFrame section="programmaticSimulator">
      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(0,0.95fr)]">
        <form onSubmit={handleSubmit} className="panel space-y-5 rounded-[28px] p-5 sm:p-6">
          <div className="rounded-3xl border border-[var(--panel-border)] bg-[var(--accent-soft)] px-4 py-3 text-sm leading-6 text-[var(--foreground)]">
            {labels.note}
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <label className="flex flex-col gap-1.5">
              <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.advertiser} <span className="text-[var(--muted)]/80">({labels.optional})</span>
              </span>
              <input
                list="simulator-advertisers"
                value={advertiserId}
                onChange={(event) => setAdvertiserId(event.target.value)}
                placeholder={labels.advertiserPlaceholder}
                className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
              />
            </label>

            <div className="rounded-2xl border border-[var(--panel-border)] bg-white/40 px-3.5 py-3 text-xs text-[var(--muted)] dark:bg-white/5">
              <p className="font-semibold uppercase tracking-[0.18em] text-[var(--foreground)]">{labels.existingAdvertisers}</p>
              <p className="mt-2">
                {advertisersState.status === "loading"
                  ? dictionary.common.loading
                  : advertisersState.status === "error"
                    ? advertisersState.message
                    : advertiserOptions.length > 0
                      ? advertiserOptions.map((item) => item.name).join(", ")
                      : dictionary.common.noData}
              </p>
            </div>

            <label className="flex flex-col gap-1.5">
              <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.dateFrom}
              </span>
              <input
                type="date"
                value={dateFrom}
                onChange={(event) => setDateFrom(event.target.value)}
                className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
                required
              />
            </label>

            <label className="flex flex-col gap-1.5">
              <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.dateTo}
              </span>
              <input
                type="date"
                value={dateTo}
                onChange={(event) => setDateTo(event.target.value)}
                className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
                required
              />
            </label>

            <label className="md:col-span-2 flex flex-col gap-1.5">
              <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.cities}
              </span>
              <input
                value={cities}
                onChange={(event) => setCities(event.target.value)}
                placeholder="Lisbon, Porto"
                className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
              />
              <span className="text-xs text-[var(--muted)]">{labels.citiesHelp}</span>
            </label>
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <fieldset className="rounded-3xl border border-[var(--panel-border)] p-4">
              <legend className="px-1 text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.buildingTypes}
              </legend>
              <div className="mt-3 grid grid-cols-1 gap-2 sm:grid-cols-2">
                {DELIVERY_BUILDING_TYPES.map((item) => (
                  <label
                    key={item}
                    className="flex items-center gap-2 rounded-2xl border border-[var(--panel-border)] px-3 py-2 text-sm text-[var(--foreground)]"
                  >
                    <input
                      type="checkbox"
                      checked={buildingTypes.includes(item)}
                      onChange={() => setBuildingTypes((current) => toggleValue(current, item))}
                    />
                    <span>{item}</span>
                  </label>
                ))}
              </div>
            </fieldset>

            <fieldset className="rounded-3xl border border-[var(--panel-border)] p-4">
              <legend className="px-1 text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.screenOrientations}
              </legend>
              <div className="mt-3 grid grid-cols-1 gap-2">
                {DELIVERY_SCREEN_ORIENTATIONS.map((item) => (
                  <label
                    key={item}
                    className="flex items-center gap-2 rounded-2xl border border-[var(--panel-border)] px-3 py-2 text-sm text-[var(--foreground)]"
                  >
                    <input
                      type="checkbox"
                      checked={screenOrientations.includes(item)}
                      onChange={() => setScreenOrientations((current) => toggleValue(current, item))}
                    />
                    <span>{item}</span>
                  </label>
                ))}
              </div>
            </fieldset>
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <label className="flex flex-col gap-1.5">
              <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.creativeDurationSeconds}
              </span>
              <input
                type="number"
                min={1}
                value={creativeDurationSeconds}
                onChange={(event) => setCreativeDurationSeconds(event.target.value)}
                className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
                required
              />
            </label>

            <label className="flex flex-col gap-1.5">
              <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.budget} <span className="text-[var(--muted)]/80">({labels.optional})</span>
              </span>
              <input
                type="number"
                min={0}
                step="0.01"
                value={budget}
                onChange={(event) => setBudget(event.target.value)}
                className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
              />
            </label>

            <label className="md:col-span-2 flex flex-col gap-1.5">
              <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.campaignObjective} <span className="text-[var(--muted)]/80">({labels.optional})</span>
              </span>
              <input
                value={campaignObjective}
                onChange={(event) => setCampaignObjective(event.target.value)}
                placeholder={labels.objectivePlaceholder}
                className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
              />
            </label>

            <label className="md:col-span-2 flex flex-col gap-1.5">
              <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.notes} <span className="text-[var(--muted)]/80">({labels.optional})</span>
              </span>
              <textarea
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
                placeholder={labels.notesPlaceholder}
                rows={4}
                className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
              />
            </label>
          </div>

          {localError ? (
            <div className="rounded-2xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-[var(--foreground)]">
              {localError}
            </div>
          ) : null}

          <button
            type="submit"
            disabled={resultState.status === "loading"}
            className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {resultState.status === "loading" ? labels.runningForecast : labels.runForecast}
          </button>

          <datalist id="simulator-advertisers">
            {advertiserOptions.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </datalist>
        </form>

        <section className="space-y-4">
          {resultState.status === "idle" ? (
            <div className="panel rounded-[28px] p-6">
              <h2 className="text-xl font-semibold tracking-[-0.03em] text-[var(--foreground)]">{labels.emptyTitle}</h2>
              <p className="mt-3 text-sm leading-6 text-[var(--muted)]">{labels.emptyDescription}</p>
            </div>
          ) : null}

          {resultState.status === "loading" ? <LoadingState /> : null}

          {resultState.status === "error" ? (
            <ErrorState message={resultState.message} onRetry={retryLastRun} />
          ) : null}

          {resultState.status === "result" ? (
            <div className="space-y-4">
              <div className="panel rounded-[28px] p-5">
                <h2 className="text-xl font-semibold tracking-[-0.03em] text-[var(--foreground)]">{labels.resultsTitle}</h2>
                <p className="mt-2 text-sm leading-6 text-[var(--muted)]">{labels.audienceEstimate}</p>
                <p className="text-sm leading-6 text-[var(--muted)]">{labels.capacityEstimate}</p>
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <SummaryCard
                  label={labels.eligibleScreens}
                  value={resultState.data.eligibleScreens.toLocaleString()}
                />
                <SummaryCard
                  label={labels.eligibleBuildings}
                  value={resultState.data.eligibleBuildings.toLocaleString()}
                />
                <SummaryCard
                  label={labels.estimatedPlays}
                  value={resultState.data.estimatedPlays.toLocaleString()}
                />
                <SummaryCard
                  label={labels.estimatedAudience}
                  value={resultState.data.estimatedAudience.toLocaleString()}
                />
                <SummaryCard
                  label={labels.estimatedCost}
                  value={new Intl.NumberFormat(undefined, { style: "currency", currency: "EUR" }).format(resultState.data.estimatedCost)}
                />
                <SummaryCard
                  label={labels.availableCapacity}
                  value={formatCapacity(resultState.data.availableCapacity)}
                />
              </div>

              {resultState.data.warnings.length > 0 ? (
                <div className="rounded-[28px] border border-amber-500/25 bg-amber-500/10 p-5">
                  <h3 className="text-sm font-semibold uppercase tracking-[0.22em] text-[var(--foreground)]">
                    {labels.warnings}
                  </h3>
                  <ul className="mt-3 space-y-2 text-sm leading-6 text-[var(--foreground)]">
                    {resultState.data.warnings.map((item) => (
                      <li key={item}>• {item}</li>
                    ))}
                  </ul>
                </div>
              ) : null}

              {resultState.data.conflicts.length > 0 ? (
                <div className="rounded-[28px] border border-rose-500/25 bg-rose-500/10 p-5">
                  <h3 className="text-sm font-semibold uppercase tracking-[0.22em] text-[var(--foreground)]">
                    {labels.conflicts}
                  </h3>
                  <ul className="mt-3 space-y-2 text-sm leading-6 text-[var(--foreground)]">
                    {resultState.data.conflicts.map((item) => (
                      <li key={item}>• {item}</li>
                    ))}
                  </ul>
                </div>
              ) : null}

              <div className="rounded-[28px] border border-emerald-500/25 bg-emerald-500/10 p-5 text-sm leading-6 text-[var(--foreground)]">
                <h3 className="text-sm font-semibold uppercase tracking-[0.22em]">{labels.suggestedNextAction}</h3>
                <p className="mt-3">{resultState.data.suggestedNextAction}</p>
              </div>
            </div>
          ) : null}
        </section>
      </div>
    </ClientPageFrame>
  );
}
