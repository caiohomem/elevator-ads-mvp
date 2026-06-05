"use client";

import { useEffect, useMemo, useState } from "react";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { EmptyState } from "@/components/EmptyState";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { SummaryCard } from "@/components/SummaryCard";
import { getAdvertiserCampaignReport, getAdvertisers, getCampaigns } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import type {
  Advertiser,
  AdvertiserCampaignCreativeSummary,
  AdvertiserCampaignDailyBreakdown,
  AdvertiserCampaignReport,
  Campaign,
} from "@/lib/types";

type ReportState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "ok"; data: AdvertiserCampaignReport };

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
  return `${campaign.name} · ${campaign.status}`;
}

export default function AdvertiserCampaignReportPage() {
  const initialRange = defaultRange();
  const advertisersState = useApiData<Advertiser[]>(getAdvertisers);
  const campaignsState = useApiData<Campaign[]>(getCampaigns);
  const [advertiserId, setAdvertiserId] = useState("");
  const [campaignId, setCampaignId] = useState("");
  const [fromInput, setFromInput] = useState(initialRange.from);
  const [toInput, setToInput] = useState(initialRange.to);
  const [reportState, setReportState] = useState<ReportState>({ status: "idle" });

  useEffect(() => {
    if (advertisersState.status !== "ok" || advertiserId) {
      return;
    }

    setAdvertiserId(advertisersState.data[0]?.id ?? "");
  }, [advertiserId, advertisersState]);

  const advertiserCampaigns = useMemo(() => {
    if (campaignsState.status !== "ok" || !advertiserId) {
      return [];
    }

    return campaignsState.data.filter((campaign) => campaign.advertiserId === advertiserId);
  }, [advertiserId, campaignsState]);

  useEffect(() => {
    if (!advertiserId) {
      setCampaignId("");
      return;
    }

    if (advertiserCampaigns.some((campaign) => campaign.id === campaignId)) {
      return;
    }

    setCampaignId(advertiserCampaigns[0]?.id ?? "");
  }, [advertiserCampaigns, advertiserId, campaignId]);

  const selectedAdvertiser = advertisersState.status === "ok"
    ? advertisersState.data.find((item) => item.id === advertiserId) ?? null
    : null;

  const selectedCampaign = advertiserCampaigns.find((item) => item.id === campaignId) ?? null;

  const creativeColumns = useMemo<TableColumn<AdvertiserCampaignCreativeSummary>[]>(() => [
    {
      key: "creativeName",
      header: "Creative / Criativo",
      render: (row) => row.creativeName,
    },
    {
      key: "mediaType",
      header: "Media type / Midia",
      render: (row) => row.mediaType,
    },
    {
      key: "durationSeconds",
      header: "Duration / Duracao",
      render: (row) => `${formatInteger(row.durationSeconds)}s`,
      className: "text-right",
    },
    {
      key: "totalPlays",
      header: "Total plays / Exibicoes",
      render: (row) => formatInteger(row.totalPlays),
      className: "text-right",
    },
    {
      key: "estimatedImpressions",
      header: "Estimated impressions / Impressoes estimadas",
      render: (row) => formatInteger(row.estimatedImpressions),
      className: "text-right",
    },
  ], []);

  const dailyColumns = useMemo<TableColumn<AdvertiserCampaignDailyBreakdown>[]>(() => [
    {
      key: "date",
      header: "Date / Data",
      render: (row) => <span className="font-mono text-xs">{row.date}</span>,
    },
    {
      key: "totalPlays",
      header: "Total plays / Exibicoes",
      render: (row) => formatInteger(row.totalPlays),
      className: "text-right",
    },
    {
      key: "estimatedAudience",
      header: "Estimated audience / Audiencia estimada",
      render: (row) => formatInteger(row.estimatedAudience),
      className: "text-right",
    },
    {
      key: "estimatedImpressions",
      header: "Estimated impressions / Impressoes estimadas",
      render: (row) => formatInteger(row.estimatedImpressions),
      className: "text-right",
    },
    {
      key: "screensCount",
      header: "Screens / Telas",
      render: (row) => formatInteger(row.screensCount),
      className: "text-right",
    },
    {
      key: "buildingsCount",
      header: "Buildings / Edificios",
      render: (row) => formatInteger(row.buildingsCount),
      className: "text-right",
    },
  ], []);

  async function handleGenerate(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!advertiserId || !campaignId || !fromInput || !toInput) {
      return;
    }

    setReportState({ status: "loading" });
    const result = await getAdvertiserCampaignReport(advertiserId, campaignId, fromInput, toInput);
    setReportState(result.ok ? { status: "ok", data: result.data } : { status: "error", message: result.message });
  }

  const content = (() => {
    if (advertisersState.status === "loading" || campaignsState.status === "loading") {
      return <LoadingState />;
    }

    if (advertisersState.status === "error") {
      return <ErrorState message={advertisersState.message} onRetry={advertisersState.retry} />;
    }

    if (campaignsState.status === "error") {
      return <ErrorState message={campaignsState.message} onRetry={campaignsState.retry} />;
    }

    if (advertisersState.data.length === 0) {
      return (
        <div className="rounded-[28px] border border-dashed border-[var(--panel-border)] px-6 py-12 text-center text-sm text-[var(--muted)]">
          No advertisers available yet. Create an advertiser before generating this report.
        </div>
      );
    }

    return (
      <div className="space-y-6">
        <form
          onSubmit={handleGenerate}
          className="panel grid gap-4 rounded-[28px] px-5 py-5 xl:grid-cols-[minmax(0,1.1fr)_minmax(0,1.4fr)_minmax(0,0.8fr)_minmax(0,0.8fr)_auto]"
        >
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="advertiser-report-advertiser"
              className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]"
            >
              Advertiser / Anunciante
            </label>
            <select
              id="advertiser-report-advertiser"
              value={advertiserId}
              onChange={(event) => {
                setAdvertiserId(event.target.value);
                setReportState({ status: "idle" });
              }}
              className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
            >
              {advertisersState.data.map((advertiser) => (
                <option key={advertiser.id} value={advertiser.id}>
                  {advertiser.name}
                </option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="advertiser-report-campaign"
              className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]"
            >
              Campaign / Campanha
            </label>
            <select
              id="advertiser-report-campaign"
              value={campaignId}
              onChange={(event) => {
                setCampaignId(event.target.value);
                setReportState({ status: "idle" });
              }}
              disabled={advertiserCampaigns.length === 0}
              className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-white/5"
            >
              {advertiserCampaigns.length > 0 ? advertiserCampaigns.map((campaign) => (
                <option key={campaign.id} value={campaign.id}>
                  {buildCampaignLabel(campaign)}
                </option>
              )) : (
                <option value="">No campaigns for this advertiser</option>
              )}
            </select>
          </div>
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="advertiser-report-from"
              className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]"
            >
              From / De
            </label>
            <input
              id="advertiser-report-from"
              type="date"
              value={fromInput}
              onChange={(event) => {
                setFromInput(event.target.value);
                setReportState({ status: "idle" });
              }}
              className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="advertiser-report-to"
              className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]"
            >
              To / Ate
            </label>
            <input
              id="advertiser-report-to"
              type="date"
              value={toInput}
              onChange={(event) => {
                setToInput(event.target.value);
                setReportState({ status: "idle" });
              }}
              className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
            />
          </div>
          <button
            type="submit"
            disabled={!advertiserId || !campaignId || !fromInput || !toInput || reportState.status === "loading"}
            className="rounded-2xl bg-[var(--accent)] px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {reportState.status === "loading" ? "Generating..." : "Generate report / Gerar relatorio"}
          </button>
        </form>

        <section className="panel relative overflow-hidden rounded-[32px] p-6 sm:p-7">
          <div className="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-[var(--accent)]/45 to-transparent" />
          <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
            <div className="space-y-3">
              <span className="inline-flex rounded-full border border-[var(--panel-border)] bg-white/60 px-3 py-1 text-[0.68rem] font-semibold uppercase tracking-[0.24em] text-[var(--muted)] dark:bg-white/[0.04]">
                Advertiser campaign report / Relatorio de campanha do anunciante
              </span>
              <div className="space-y-2">
                <h2 className="text-2xl font-semibold tracking-[-0.05em] text-[var(--foreground)]">
                  Delivery results framed for advertiser communication.
                </h2>
                <p className="max-w-3xl text-sm leading-6 text-[var(--muted)]">
                  Plays are separated from audience and impression estimates so the report stays business-friendly without implying measured passenger counting where only modeled estimates exist.
                </p>
              </div>
            </div>
            <div className="grid gap-2 text-sm text-[var(--muted)] sm:grid-cols-2">
              <div className="rounded-2xl border border-[var(--panel-border)] bg-white/50 px-4 py-3 dark:bg-white/[0.03]">
                <div className="text-[0.68rem] font-semibold uppercase tracking-[0.22em]">Advertiser</div>
                <div className="mt-2 font-medium text-[var(--foreground)]">{selectedAdvertiser?.name ?? "-"}</div>
              </div>
              <div className="rounded-2xl border border-[var(--panel-border)] bg-white/50 px-4 py-3 dark:bg-white/[0.03]">
                <div className="text-[0.68rem] font-semibold uppercase tracking-[0.22em]">Campaign / Campanha</div>
                <div className="mt-2 font-medium text-[var(--foreground)]">{selectedCampaign ? buildCampaignLabel(selectedCampaign) : "-"}</div>
              </div>
            </div>
          </div>
        </section>

        {advertiserCampaigns.length === 0 ? (
          <div className="rounded-[28px] border border-dashed border-[var(--panel-border)] px-6 py-12 text-center text-sm text-[var(--muted)]">
            This advertiser has no campaigns yet. Create a campaign before generating the report.
          </div>
        ) : null}

        {reportState.status === "idle" && advertiserCampaigns.length > 0 ? (
          <div className="rounded-[28px] border border-dashed border-[var(--panel-border)] px-6 py-12 text-center text-sm text-[var(--muted)]">
            Choose an advertiser, campaign, and date range, then generate the report to review delivery, creative performance, and daily estimates.
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
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <SummaryCard label="Total plays / Exibicoes totais" value={formatInteger(reportState.data.totalPlays)} />
              <SummaryCard label="Scheduled plays / Exibicoes programadas" value={formatInteger(reportState.data.totalScheduledPlays)} />
              <SummaryCard label="Reported plays / Exibicoes reportadas" value={formatInteger(reportState.data.totalReportedPlays)} />
              <SummaryCard label="Estimated audience / Audiencia estimada" value={formatInteger(reportState.data.estimatedAudience)} />
              <SummaryCard label="Estimated impressions / Impressoes estimadas" value={formatInteger(reportState.data.estimatedImpressions)} />
              <SummaryCard label="Screens / Telas" value={formatInteger(reportState.data.screensCount)} />
              <SummaryCard label="Buildings / Edificios" value={formatInteger(reportState.data.buildingsCount)} />
              <SummaryCard label="Status" value={reportState.data.status} description={`${reportState.data.dateFrom} to ${reportState.data.dateTo}`} />
            </div>

            <div className="grid gap-4 lg:grid-cols-[minmax(0,1.5fr)_minmax(20rem,0.9fr)]">
              <section className="space-y-3">
                <div className="space-y-1">
                  <h3 className="text-lg font-semibold tracking-[-0.02em] text-[var(--foreground)]">
                    Creative performance / Desempenho dos criativos
                  </h3>
                  <p className="text-sm text-[var(--muted)]">
                    Each creative shows effective plays and the related estimated impressions for the selected range.
                  </p>
                </div>
                {reportState.data.creatives.length > 0 ? (
                  <DataTable
                    columns={creativeColumns}
                    rows={reportState.data.creatives}
                    getRowKey={(row) => row.creativeId}
                  />
                ) : (
                  <div className="space-y-3">
                    <EmptyState />
                    <p className="text-sm text-[var(--muted)]">
                      No creatives matched this advertiser campaign and date range.
                    </p>
                  </div>
                )}
              </section>

              <section className="panel rounded-[28px] p-5">
                <div className="space-y-1">
                  <h3 className="text-base font-semibold tracking-[-0.02em] text-[var(--foreground)]">
                    Coverage / Cobertura
                  </h3>
                  <p className="text-sm text-[var(--muted)]">
                    Advertiser, campaign, and city footprint included in this report window.
                  </p>
                </div>
                <div className="mt-4 space-y-3 text-sm text-[var(--foreground)]">
                  <div className="rounded-2xl border border-[var(--panel-border)] bg-white/45 px-4 py-3 dark:bg-white/[0.03]">
                    <div className="text-[0.68rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">Advertiser / Anunciante</div>
                    <div className="mt-2 font-medium">{reportState.data.advertiserName}</div>
                  </div>
                  <div className="rounded-2xl border border-[var(--panel-border)] bg-white/45 px-4 py-3 dark:bg-white/[0.03]">
                    <div className="text-[0.68rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">Campaign / Campanha</div>
                    <div className="mt-2 font-medium">{reportState.data.campaignName}</div>
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

            <section className="space-y-3">
              <div className="space-y-1">
                <h3 className="text-lg font-semibold tracking-[-0.02em] text-[var(--foreground)]">
                  Daily breakdown / Quebra diaria
                </h3>
                <p className="text-sm text-[var(--muted)]">
                  Daily totals separate effective plays from estimated audience and impression figures.
                </p>
              </div>
              {reportState.data.dailyBreakdown.length > 0 ? (
                <DataTable
                  columns={dailyColumns}
                  rows={reportState.data.dailyBreakdown}
                  getRowKey={(row) => row.date}
                />
              ) : (
                <div className="space-y-3">
                  <EmptyState />
                  <p className="text-sm text-[var(--muted)]">
                    No daily rows were generated for this date range.
                  </p>
                </div>
              )}
            </section>

            <div className="grid gap-4 lg:grid-cols-2">
              <section className="panel rounded-[28px] p-5">
                <div className="space-y-1">
                  <h3 className="text-base font-semibold tracking-[-0.02em] text-[var(--foreground)]">
                    Assumptions / Premissas
                  </h3>
                  <p className="text-sm text-[var(--muted)]">
                    These notes explain where the report uses modeled estimates instead of direct audience measurement.
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
                    These notes highlight where proof-of-play was incomplete or estimates depend on fallback logic.
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
                    No warnings for this selection. The report matched the current MVP assumptions without fallback alerts.
                  </p>
                )}
              </section>
            </div>
          </div>
        ) : null}
      </div>
    );
  })();

  return <ClientPageFrame section="reports">{content}</ClientPageFrame>;
}
