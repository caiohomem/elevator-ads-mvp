"use client";

import { useCallback, useState } from "react";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { SummaryCard } from "@/components/SummaryCard";
import {
  getBookingRequestsPaged,
  getCampaignsList,
  getInventoryPackagesPaged,
  runPlaylistSimulate,
} from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { useTranslation } from "@/lib/i18n";
import type {
  ApiBookingRequest,
  ApiCampaign,
  ApiInventoryPackage,
  PlaylistSimulateRequest,
  PlaylistSimulateResponse,
} from "@/lib/types";

type SourceType = "none" | "bookingRequest" | "campaign" | "inventoryPackage";

type ResultState =
  | { status: "idle" }
  | { status: "loading"; payload: PlaylistSimulateRequest }
  | { status: "error"; message: string; payload: PlaylistSimulateRequest }
  | { status: "result"; data: PlaylistSimulateResponse; payload: PlaylistSimulateRequest };

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

function formatLoopDuration(seconds: number) {
  if (seconds < 60) {
    return `${seconds}s`;
  }

  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return remainingSeconds > 0 ? `${minutes}m ${remainingSeconds}s` : `${minutes}m`;
}

export default function PlaylistSimulatorPage() {
  const { dictionary } = useTranslation();
  const labels = dictionary.playlistSimulator;
  const forms = dictionary.forms;

  const bookingRequestsFetcher = useCallback(
    () => getBookingRequestsPaged({ page: 1, pageSize: 100, sortBy: "createdAt", sortDirection: "desc" }),
    [],
  );
  const inventoryPackagesFetcher = useCallback(
    () => getInventoryPackagesPaged({ page: 1, pageSize: 100, sortBy: "createdAt", sortDirection: "desc" }),
    [],
  );

  const campaignsState = useApiData(getCampaignsList);
  const bookingRequestsState = useApiData(bookingRequestsFetcher);
  const inventoryPackagesState = useApiData(inventoryPackagesFetcher);

  const campaigns: ApiCampaign[] = campaignsState.status === "ok" ? campaignsState.data : [];
  const bookingRequests: ApiBookingRequest[] = bookingRequestsState.status === "ok" ? bookingRequestsState.data.items : [];
  const inventoryPackages: ApiInventoryPackage[] = inventoryPackagesState.status === "ok" ? inventoryPackagesState.data.items : [];

  const [sourceType, setSourceType] = useState<SourceType>("none");
  const [sourceId, setSourceId] = useState("");
  const [date, setDate] = useState(todayIsoDate);
  const [operatingHoursPerDay, setOperatingHoursPerDay] = useState("16");
  const [creativeDurationSeconds, setCreativeDurationSeconds] = useState("15");
  const [maxLoopDurationSeconds, setMaxLoopDurationSeconds] = useState("");
  const [localError, setLocalError] = useState<string | null>(null);
  const [resultState, setResultState] = useState<ResultState>({ status: "idle" });

  const handleSourceTypeChange = (nextSourceType: SourceType) => {
    setSourceType(nextSourceType);
    setSourceId("");
  };

  const handleSourceIdChange = (nextSourceId: string) => {
    setSourceId(nextSourceId);

    if (sourceType === "bookingRequest") {
      const selectedBookingRequest = bookingRequests.find((item) => item.id === nextSourceId);
      if (selectedBookingRequest) {
        setCreativeDurationSeconds(String(selectedBookingRequest.creativeDurationSeconds));
      }
    }
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setLocalError(null);

    const duration = Number(creativeDurationSeconds);
    const hours = Number(operatingHoursPerDay);
    const maxLoopDuration = maxLoopDurationSeconds.trim() ? Number(maxLoopDurationSeconds) : null;

    if (!Number.isFinite(duration) || duration < 1) {
      setLocalError(forms.mustBeGreaterThanZero);
      return;
    }

    if (!Number.isFinite(hours) || hours <= 0 || hours > 24) {
      setLocalError(labels.operatingHoursRange);
      return;
    }

    if (maxLoopDuration !== null && (!Number.isFinite(maxLoopDuration) || maxLoopDuration < 1)) {
      setLocalError(forms.mustBeGreaterThanZero);
      return;
    }

    if (sourceType !== "none" && !sourceId) {
      setLocalError(labels.selectSource);
      return;
    }

    const payload: PlaylistSimulateRequest = {
      bookingRequestId: sourceType === "bookingRequest" ? sourceId : null,
      campaignId: sourceType === "campaign" ? sourceId : null,
      inventoryPackageId: sourceType === "inventoryPackage" ? sourceId : null,
      date,
      screenIds: null,
      creativeDurationSeconds: duration,
      operatingHoursPerDay: hours,
      maxLoopDurationSeconds: maxLoopDuration,
    };

    setResultState({ status: "loading", payload });
    const result = await runPlaylistSimulate(payload);

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
    const result = await runPlaylistSimulate(resultState.payload);

    if (!result.ok) {
      setResultState({ status: "error", message: result.message, payload: resultState.payload });
      return;
    }

    setResultState({ status: "result", data: result.data, payload: resultState.payload });
  };

  return (
    <ClientPageFrame section="playlistSimulator">
      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(0,0.95fr)]">
        <form onSubmit={handleSubmit} className="panel space-y-5 rounded-[28px] p-5 sm:p-6">
          <div className="rounded-3xl border border-[var(--panel-border)] bg-[var(--accent-soft)] px-4 py-3 text-sm leading-6 text-[var(--foreground)]">
            {labels.note}
          </div>

          <label className="flex flex-col gap-1.5">
            <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
              {labels.sourceType}
            </span>
            <select
              value={sourceType}
              onChange={(event) => handleSourceTypeChange(event.target.value as SourceType)}
              className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
            >
              <option value="none">{labels.none}</option>
              <option value="bookingRequest">{labels.bookingRequest}</option>
              <option value="campaign">{labels.campaign}</option>
              <option value="inventoryPackage">{labels.inventoryPackage}</option>
            </select>
            <span className="text-xs text-[var(--muted)]">{labels.sourceHelp}</span>
          </label>

          {sourceType === "campaign" ? (
            <SourceSelect
              label={labels.sourceReference}
              value={sourceId}
              onChange={handleSourceIdChange}
              items={campaigns.map((item) => ({ id: item.id, name: item.name }))}
              status={campaignsState.status}
              errorMessage={campaignsState.status === "error" ? campaignsState.message : undefined}
              labels={labels}
            />
          ) : null}

          {sourceType === "bookingRequest" ? (
            <SourceSelect
              label={labels.sourceReference}
              value={sourceId}
              onChange={handleSourceIdChange}
              items={bookingRequests.map((item) => ({ id: item.id, name: item.name }))}
              status={bookingRequestsState.status}
              errorMessage={bookingRequestsState.status === "error" ? bookingRequestsState.message : undefined}
              labels={labels}
            />
          ) : null}

          {sourceType === "inventoryPackage" ? (
            <SourceSelect
              label={labels.sourceReference}
              value={sourceId}
              onChange={handleSourceIdChange}
              items={inventoryPackages.map((item) => ({ id: item.id, name: item.name }))}
              status={inventoryPackagesState.status}
              errorMessage={inventoryPackagesState.status === "error" ? inventoryPackagesState.message : undefined}
              labels={labels}
            />
          ) : null}

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <label className="flex flex-col gap-1.5">
              <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.date}
              </span>
              <input
                type="date"
                value={date}
                onChange={(event) => setDate(event.target.value)}
                className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
                required
              />
            </label>

            <label className="flex flex-col gap-1.5">
              <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                {labels.operatingHoursPerDay}
              </span>
              <input
                type="number"
                min={1}
                max={24}
                step="0.5"
                value={operatingHoursPerDay}
                onChange={(event) => setOperatingHoursPerDay(event.target.value)}
                className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
                required
              />
            </label>

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
                {labels.maxLoopDurationSeconds} <span className="text-[var(--muted)]/80">({labels.optional})</span>
              </span>
              <input
                type="number"
                min={1}
                value={maxLoopDurationSeconds}
                onChange={(event) => setMaxLoopDurationSeconds(event.target.value)}
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
            {resultState.status === "loading" ? labels.runningSimulation : labels.runSimulation}
          </button>
        </form>

        <section className="space-y-4">
          {resultState.status === "idle" ? (
            <div className="panel rounded-[28px] p-6">
              <h2 className="text-xl font-semibold tracking-[-0.03em] text-[var(--foreground)]">{labels.emptyTitle}</h2>
              <p className="mt-3 text-sm leading-6 text-[var(--muted)]">{labels.emptyDescription}</p>
            </div>
          ) : null}

          {resultState.status === "loading" ? <LoadingState /> : null}
          {resultState.status === "error" ? <ErrorState message={resultState.message} onRetry={retryLastRun} /> : null}

          {resultState.status === "result" ? (
            <div className="space-y-4">
              <div className="panel rounded-[28px] p-5">
                <h2 className="text-xl font-semibold tracking-[-0.03em] text-[var(--foreground)]">{labels.resultsTitle}</h2>
                <p className="mt-2 text-sm leading-6 text-[var(--muted)]">{labels.summaryNote}</p>
                <p className="mt-1 text-sm leading-6 text-[var(--muted)]">
                  {labels.dateLabel}: <span className="font-mono text-[var(--foreground)]">{resultState.data.date}</span>
                </p>
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <SummaryCard label={labels.loopDurationSeconds} value={formatLoopDuration(resultState.data.loopDurationSeconds)} />
                <SummaryCard label={labels.estimatedLoopsPerDay} value={resultState.data.estimatedLoopsPerDay.toLocaleString()} />
                <SummaryCard label={labels.estimatedPlaysPerCreative} value={resultState.data.estimatedPlaysPerCreative.toLocaleString()} />
                <SummaryCard label={labels.estimatedTotalPlays} value={resultState.data.estimatedTotalPlays.toLocaleString()} />
                <SummaryCard label={labels.estimatedAudience} value={resultState.data.estimatedAudience.toLocaleString()} />
                <SummaryCard label={labels.eligibleScreens} value={resultState.data.eligibleScreens.toLocaleString()} />
                <SummaryCard label={labels.eligibleBuildings} value={resultState.data.eligibleBuildings.toLocaleString()} />
              </div>

              <div className="panel rounded-[28px] p-5">
                <h3 className="text-sm font-semibold uppercase tracking-[0.22em] text-[var(--foreground)]">{labels.itemsTitle}</h3>
                {resultState.data.items.length === 0 ? (
                  <p className="mt-4 text-sm text-[var(--muted)]">{labels.itemsEmpty}</p>
                ) : (
                  <div className="mt-4 overflow-x-auto">
                    <table className="min-w-full text-left text-sm">
                      <thead className="text-[0.72rem] uppercase tracking-[0.18em] text-[var(--muted)]">
                        <tr>
                          <th className="pb-3 pr-4">{labels.itemOrder}</th>
                          <th className="pb-3 pr-4">{labels.itemDuration}</th>
                          <th className="pb-3 pr-4">{labels.itemSource}</th>
                          <th className="pb-3">{labels.itemNotes}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {resultState.data.items.map((item) => (
                          <tr key={`${item.order}-${item.source}`} className="border-t border-[var(--panel-border)]">
                            <td className="py-3 pr-4 font-mono text-[var(--foreground)]">{item.order}</td>
                            <td className="py-3 pr-4 font-mono text-[var(--foreground)]">{item.creativeDurationSeconds}</td>
                            <td className="py-3 pr-4 text-[var(--foreground)]">{item.source}</td>
                            <td className="py-3 text-[var(--muted)]">{item.notes ?? "-"}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>

              {resultState.data.warnings.length > 0 ? (
                <div className="rounded-[28px] border border-amber-500/25 bg-amber-500/10 p-5">
                  <h3 className="text-sm font-semibold uppercase tracking-[0.22em] text-[var(--foreground)]">{labels.warnings}</h3>
                  <ul className="mt-3 space-y-2 text-sm leading-6 text-[var(--foreground)]">
                    {resultState.data.warnings.map((item) => (
                      <li key={item}>• {item}</li>
                    ))}
                  </ul>
                </div>
              ) : null}

              {resultState.data.conflicts.length > 0 ? (
                <div className="rounded-[28px] border border-rose-500/25 bg-rose-500/10 p-5">
                  <h3 className="text-sm font-semibold uppercase tracking-[0.22em] text-[var(--foreground)]">{labels.conflicts}</h3>
                  <ul className="mt-3 space-y-2 text-sm leading-6 text-[var(--foreground)]">
                    {resultState.data.conflicts.map((item) => (
                      <li key={item}>• {item}</li>
                    ))}
                  </ul>
                </div>
              ) : null}
            </div>
          ) : null}
        </section>
      </div>
    </ClientPageFrame>
  );
}

function SourceSelect({
  label,
  value,
  onChange,
  items,
  status,
  errorMessage,
  labels,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  items: Array<{ id: string; name: string }>;
  status: "loading" | "error" | "ok";
  errorMessage?: string;
  labels: {
    selectSource: string;
    sourceLoading: string;
    noSourcesAvailable: string;
  };
}) {
  return (
    <label className="flex flex-col gap-1.5">
      <span className="text-[0.72rem] font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">{label}</span>
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={status !== "ok"}
        className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-white/5"
      >
        <option value="">
          {status === "loading"
            ? labels.sourceLoading
            : status === "error"
              ? errorMessage ?? labels.noSourcesAvailable
              : labels.selectSource}
        </option>
        {items.map((item) => (
          <option key={item.id} value={item.id}>
            {item.name}
          </option>
        ))}
      </select>
      {status === "ok" && items.length === 0 ? <span className="text-xs text-[var(--muted)]">{labels.noSourcesAvailable}</span> : null}
    </label>
  );
}
