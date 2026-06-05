"use client";

import { useEffect, useMemo, useState } from "react";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { EmptyState } from "@/components/EmptyState";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { SummaryCard } from "@/components/SummaryCard";
import { getCampaigns } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { getEstimatedProofOfPlay } from "@/lib/api/estimated-proof-of-play";
import type { Campaign, EstimatedProofOfPlayItem, EstimatedProofOfPlayReport } from "@/lib/types";

type ReportState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "ok"; data: EstimatedProofOfPlayReport };

function toIsoDate(value: Date): string {
  return value.toISOString().slice(0, 10);
}

function defaultRange(): { from: string; to: string } {
  const today = new Date();
  const sevenDaysAgo = new Date(today);
  sevenDaysAgo.setDate(today.getDate() - 7);
  return { from: toIsoDate(sevenDaysAgo), to: toIsoDate(today) };
}

function formatInteger(value: number): string {
  return value.toLocaleString();
}

function buildCampaignLabel(campaign: Campaign): string {
  return `${campaign.name} · ${campaign.advertiserName}`;
}

export default function EstimatedProofOfPlayPage() {
  const initialRange = defaultRange();
  const campaignsState = useApiData<Campaign[]>(getCampaigns);
  const [campaignId, setCampaignId] = useState("");
  const [fromInput, setFromInput] = useState(initialRange.from);
  const [toInput, setToInput] = useState(initialRange.to);
  const [reportState, setReportState] = useState<ReportState>({ status: "idle" });

  useEffect(() => {
    if (campaignsState.status !== "ok" || campaignId) {
      return;
    }

    setCampaignId(campaignsState.data[0]?.id ?? "");
  }, [campaignId, campaignsState]);

  const selectedCampaign = campaignsState.status === "ok"
    ? campaignsState.data.find((item) => item.id === campaignId) ?? null
    : null;

  const itemColumns = useMemo<TableColumn<EstimatedProofOfPlayItem>[]>(() => [
    {
      key: "date",
      header: "Date / Data",
      render: (row) => <span className="font-mono text-xs">{row.date}</span>,
    },
    {
      key: "screenName",
      header: "Screen / Tela",
      render: (row) => row.screenName,
    },
    {
      key: "buildingName",
      header: "Building / Edificio",
      render: (row) => row.buildingName,
    },
    {
      key: "city",
      header: "City / Cidade",
      render: (row) => row.city,
    },
    {
      key: "creativeName",
      header: "Creative / Criativo",
      render: (row) => row.creativeName,
    },
    {
      key: "scheduledPlays",
      header: "Scheduled / Programadas",
      render: (row) => formatInteger(row.scheduledPlays),
      className: "text-right",
    },
    {
      key: "reportedPlays",
      header: "Reported / Reportadas",
      render: (row) => formatInteger(row.reportedPlays),
      className: "text-right",
    },
    {
      key: "estimatedAudience",
      header: "Audience / Audiencia",
      render: (row) => formatInteger(row.estimatedAudience),
      className: "text-right",
    },
    {
      key: "estimatedImpressions",
      header: "Impressions / Impressoes",
      render: (row) => formatInteger(row.estimatedImpressions),
      className: "text-right",
    },
  ], []);

  async function handleGenerate(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!campaignId || !fromInput || !toInput) {
      return;
    }

    setReportState({ status: "loading" });
    const result = await getEstimatedProofOfPlay(campaignId, fromInput, toInput);
    setReportState(result.ok ? { status: "ok", data: result.data } : { status: "error", message: result.message });
  }

  const content = (() => {
    if (campaignsState.status === "loading") {
      return <LoadingState />;
    }

    if (campaignsState.status === "error") {
      return <ErrorState message={campaignsState.message} onRetry={campaignsState.retry} />;
    }

    if (campaignsState.data.length === 0) {
      return (
        <div className="rounded-[28px] border border-dashed border-[var(--panel-border)] px-6 py-12 text-center text-sm text-[var(--muted)]">
          No campaigns available yet. Create a campaign before generating this report.
        </div>
      );
    }

    return (
      <div className="space-y-6">
        <form
          onSubmit={handleGenerate}
          className="panel grid gap-4 rounded-[28px] px-5 py-5 lg:grid-cols-[minmax(0,2.1fr)_minmax(0,1fr)_minmax(0,1fr)_auto]"
        >
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="estimated-pop-campaign"
              className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]"
            >
              Campaign / Campanha
            </label>
            <select
              id="estimated-pop-campaign"
              value={campaignId}
              onChange={(event) => setCampaignId(event.target.value)}
              className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
            >
              {campaignsState.data.map((campaign) => (
                <option key={campaign.id} value={campaign.id}>
                  {buildCampaignLabel(campaign)}
                </option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="estimated-pop-from"
              className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]"
            >
              From / De
            </label>
            <input
              id="estimated-pop-from"
              type="date"
              value={fromInput}
              onChange={(event) => setFromInput(event.target.value)}
              className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="estimated-pop-to"
              className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]"
            >
              To / Ate
            </label>
            <input
              id="estimated-pop-to"
              type="date"
              value={toInput}
              onChange={(event) => setToInput(event.target.value)}
              className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
            />
          </div>
          <button
            type="submit"
            disabled={!campaignId || !fromInput || !toInput || reportState.status === "loading"}
            className="rounded-2xl bg-[var(--accent)] px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {reportState.status === "loading" ? "Generating..." : "Generate report"}
          </button>
        </form>

        <section className="panel relative overflow-hidden rounded-[32px] p-6 sm:p-7">
          <div className="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-[var(--accent)]/45 to-transparent" />
          <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
            <div className="space-y-3">
              <span className="inline-flex rounded-full border border-[var(--panel-border)] bg-white/60 px-3 py-1 text-[0.68rem] font-semibold uppercase tracking-[0.24em] text-[var(--muted)] dark:bg-white/[0.04]">
                Estimated proof-of-play / Prova estimada de exibicao
              </span>
              <div className="space-y-2">
                <h2 className="text-2xl font-semibold tracking-[-0.05em] text-[var(--foreground)]">
                  Planned loops, reported plays, and audience assumptions in one view.
                </h2>
                <p className="max-w-3xl text-sm leading-6 text-[var(--muted)]">
                  Scheduled counts come from playlists, reported counts come from proof-of-play events, and audience or impressions remain estimates based on building-level daily audience fields.
                </p>
              </div>
            </div>
            <div className="grid gap-2 text-sm text-[var(--muted)] sm:grid-cols-2">
              <div className="rounded-2xl border border-[var(--panel-border)] bg-white/50 px-4 py-3 dark:bg-white/[0.03]">
                <div className="text-[0.68rem] font-semibold uppercase tracking-[0.22em]">Selected campaign</div>
                <div className="mt-2 font-medium text-[var(--foreground)]">{selectedCampaign ? buildCampaignLabel(selectedCampaign) : "-"}</div>
              </div>
              <div className="rounded-2xl border border-[var(--panel-border)] bg-white/50 px-4 py-3 dark:bg-white/[0.03]">
                <div className="text-[0.68rem] font-semibold uppercase tracking-[0.22em]">Window / Janela</div>
                <div className="mt-2 font-medium text-[var(--foreground)]">
                  {fromInput} to {toInput}
                </div>
              </div>
            </div>
          </div>
        </section>

        {reportState.status === "idle" ? (
          <div className="rounded-[28px] border border-dashed border-[var(--panel-border)] px-6 py-12 text-center text-sm text-[var(--muted)]">
            Choose a campaign and date range, then generate the report to compare actual proof-of-play with estimated audience.
          </div>
        ) : null}

        {reportState.status === "loading" ? <LoadingState /> : null}

        {reportState.status === "error" ? (
          <ErrorState
            message={reportState.message}
            onRetry={() => {
              setReportState({ status: "idle" });
            }}
          />
        ) : null}

        {reportState.status === "ok" ? (
          <div className="space-y-6">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <SummaryCard label="Scheduled plays / Exibicoes programadas" value={formatInteger(reportState.data.totalScheduledPlays)} />
              <SummaryCard label="Reported plays / Exibicoes reportadas" value={formatInteger(reportState.data.totalReportedPlays)} />
              <SummaryCard label="Estimated audience / Audiencia estimada" value={formatInteger(reportState.data.estimatedAudience)} />
              <SummaryCard label="Estimated impressions / Impressoes estimadas" value={formatInteger(reportState.data.estimatedImpressions)} />
              <SummaryCard label="Screens / Telas" value={formatInteger(reportState.data.screensCount)} />
              <SummaryCard label="Buildings / Edificios" value={formatInteger(reportState.data.buildingsCount)} />
            </div>

            <div className="grid gap-4 lg:grid-cols-[minmax(0,1.6fr)_minmax(22rem,0.9fr)]">
              <section className="space-y-3">
                <div className="space-y-1">
                  <h3 className="text-lg font-semibold tracking-[-0.02em] text-[var(--foreground)]">
                    Detail by day, screen, and creative
                  </h3>
                  <p className="text-sm text-[var(--muted)]">
                    Each row combines scheduled playlist counts, reported proof-of-play events, and the report&apos;s estimated audience assumptions.
                  </p>
                </div>
                {reportState.data.items.length > 0 ? (
                  <DataTable
                    columns={itemColumns}
                    rows={reportState.data.items}
                    getRowKey={(row) => `${row.date}-${row.screenId}-${row.creativeId}`}
                  />
                ) : (
                  <div className="space-y-3">
                    <EmptyState />
                    <p className="text-sm text-[var(--muted)]">
                      No proof-of-play events or playlist rows matched this campaign and date range.
                    </p>
                  </div>
                )}
              </section>

              <div className="space-y-4">
                <section className="panel rounded-[28px] p-5">
                  <div className="space-y-1">
                    <h3 className="text-base font-semibold tracking-[-0.02em] text-[var(--foreground)]">
                      Assumptions / Premissas
                    </h3>
                    <p className="text-sm text-[var(--muted)]">
                      This MVP keeps estimation rules explicit so advertisers can distinguish measured playback from modeled audience.
                    </p>
                  </div>
                  <ul className="mt-4 space-y-3 text-sm leading-6 text-[var(--foreground)]">
                    {reportState.data.assumptions.map((assumption) => (
                      <li key={assumption} className="rounded-2xl border border-[var(--panel-border)] bg-white/45 px-4 py-3 dark:bg-white/[0.03]">
                        {assumption}
                      </li>
                    ))}
                  </ul>
                </section>

                <section className="rounded-[28px] border border-amber-400/40 bg-amber-500/10 p-5">
                  <div className="space-y-1">
                    <h3 className="text-base font-semibold tracking-[-0.02em] text-[var(--foreground)]">
                      Warnings / Avisos
                    </h3>
                    <p className="text-sm text-[var(--muted)]">
                      These notes explain where the report depends on estimates rather than fully observed playback.
                    </p>
                  </div>
                  {reportState.data.warnings.length > 0 ? (
                    <ul className="mt-4 space-y-3 text-sm leading-6 text-[var(--foreground)]">
                      {reportState.data.warnings.map((warning) => (
                        <li key={warning} className="rounded-2xl border border-amber-400/35 bg-white/45 px-4 py-3 dark:bg-white/[0.03]">
                          {warning}
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p className="mt-4 text-sm text-[var(--foreground)]">
                      No warnings for this selection. All returned rows matched the current MVP assumptions cleanly.
                    </p>
                  )}
                </section>

                <section className="panel rounded-[28px] p-5">
                  <div className="space-y-1">
                    <h3 className="text-base font-semibold tracking-[-0.02em] text-[var(--foreground)]">
                      Coverage / Cobertura
                    </h3>
                    <p className="text-sm text-[var(--muted)]">
                      Cities represented in this result and the advertiser linked to the selected campaign.
                    </p>
                  </div>
                  <div className="mt-4 space-y-3 text-sm text-[var(--foreground)]">
                    <div className="rounded-2xl border border-[var(--panel-border)] bg-white/45 px-4 py-3 dark:bg-white/[0.03]">
                      <div className="text-[0.68rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">Advertiser / Anunciante</div>
                      <div className="mt-2 font-medium">{reportState.data.advertiserName}</div>
                    </div>
                    <div className="rounded-2xl border border-[var(--panel-border)] bg-white/45 px-4 py-3 dark:bg-white/[0.03]">
                      <div className="text-[0.68rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">Cities / Cidades</div>
                      <div className="mt-2 font-medium">
                        {reportState.data.cities.length > 0 ? reportState.data.cities.join(", ") : "No city data in the matched rows."}
                      </div>
                    </div>
                  </div>
                </section>
              </div>
            </div>
          </div>
        ) : null}
      </div>
    );
  })();

  return <ClientPageFrame section="reports">{content}</ClientPageFrame>;
}
