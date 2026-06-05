"use client";

import { useCallback, useState } from "react";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { PaginationControls } from "@/components/PaginationControls";
import { SummaryCard } from "@/components/SummaryCard";
import {
  getProofOfPlayEventsPaged,
  getReportsCampaigns,
  getReportsOverview,
  getReportsScreens,
} from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { usePagedData } from "@/lib/api/usePagedData";
import { useTranslation } from "@/lib/i18n";
import type {
  CampaignReport,
  GroupSummary,
  OverviewReport,
  ProofOfPlayEvent,
  ScreenReport,
} from "@/lib/types";

function toIsoDate(value: Date): string {
  return value.toISOString().slice(0, 10);
}

function defaultRange(): { from: string; to: string } {
  const today = new Date();
  const sevenDaysAgo = new Date(today);
  sevenDaysAgo.setDate(today.getDate() - 7);
  return { from: toIsoDate(sevenDaysAgo), to: toIsoDate(today) };
}

function formatPlayedAt(value: string): string {
  return value.replace("T", " ").slice(0, 19);
}

export default function ReportsPage() {
  const { dictionary } = useTranslation();
  const labels = dictionary.reports;

  const initialRange = defaultRange();
  const [fromInput, setFromInput] = useState<string>(initialRange.from);
  const [toInput, setToInput] = useState<string>(initialRange.to);
  const [appliedFrom, setAppliedFrom] = useState<string>(initialRange.from);
  const [appliedTo, setAppliedTo] = useState<string>(initialRange.to);

  const overviewFetcher = useCallback(
    () => getReportsOverview(appliedTo),
    [appliedTo],
  );
  const campaignFetcher = useCallback(
    () => getReportsCampaigns(appliedFrom, appliedTo),
    [appliedFrom, appliedTo],
  );
  const screenFetcher = useCallback(
    () => getReportsScreens(appliedFrom, appliedTo),
    [appliedFrom, appliedTo],
  );

  const overviewState = useApiData<OverviewReport>(overviewFetcher);
  const campaignState = useApiData<CampaignReport>(campaignFetcher);
  const screenState = useApiData<ScreenReport>(screenFetcher);
  const proofOfPlayState = usePagedData(getProofOfPlayEventsPaged, "reports-proof-of-play");

  const handleApply = (event: React.FormEvent) => {
    event.preventDefault();
    if (!fromInput || !toInput) {
      return;
    }
    setAppliedFrom(fromInput);
    setAppliedTo(toInput);
  };

  const buildGroupColumns = (idHeader: string): TableColumn<GroupSummary>[] => [
    {
      key: "id",
      header: idHeader,
      render: (row) => row.name,
    },
    {
      key: "plays",
      header: labels.plays,
      render: (row) => <span className="font-semibold">{row.plays}</span>,
    },
    {
      key: "playedSeconds",
      header: labels.playedSeconds,
      render: (row) => row.playedSeconds,
    },
  ];

  const campaignColumns = buildGroupColumns(labels.campaignId);
  const screenColumns = buildGroupColumns(labels.screenId);
  const creativeColumns = buildGroupColumns(labels.creativeId);

  const proofOfPlayColumns: TableColumn<ProofOfPlayEvent>[] = [
    {
      key: "playedAt",
      header: labels.playedAt,
      render: (row) => <span className="font-mono text-xs">{formatPlayedAt(row.playedAt)}</span>,
    },
    {
      key: "screenId",
      header: labels.screenId,
      render: (row) => row.screenName,
    },
    {
      key: "campaignId",
      header: labels.campaignId,
      render: (row) => row.campaignName,
    },
    {
      key: "creativeId",
      header: labels.creativeId,
      render: (row) => row.creativeName,
    },
    {
      key: "durationSeconds",
      header: labels.playedSeconds,
      render: (row) => row.durationSeconds,
    },
  ];

  const sectionTitle = (title: string, subtitle?: string) => (
    <div className="space-y-1">
      <h2 className="text-lg font-semibold tracking-[-0.02em] text-[var(--foreground)]">{title}</h2>
      {subtitle ? <p className="text-xs text-[var(--muted)]">{subtitle}</p> : null}
    </div>
  );

  return (
    <ClientPageFrame section="reports">
      <form
        onSubmit={handleApply}
        className="panel flex flex-col gap-3 rounded-[28px] px-5 py-4 sm:flex-row sm:items-end sm:justify-between"
      >
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="reports-from"
              className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]"
            >
              {labels.dateFrom}
            </label>
            <input
              id="reports-from"
              type="date"
              value={fromInput}
              onChange={(event) => setFromInput(event.target.value)}
              className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="reports-to"
              className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]"
            >
              {labels.dateTo}
            </label>
            <input
              id="reports-to"
              type="date"
              value={toInput}
              onChange={(event) => setToInput(event.target.value)}
              className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
            />
          </div>
        </div>
        <button
          type="submit"
          disabled={!fromInput || !toInput}
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-60"
        >
          {labels.applyFilter}
        </button>
      </form>

      <section className="space-y-4">
        {sectionTitle(labels.overviewSummary, `${labels.overviewDate}: ${appliedTo}`)}
        {overviewState.status === "loading" ? <LoadingState /> : null}
        {overviewState.status === "error" ? (
          <ErrorState message={overviewState.message} onRetry={overviewState.retry} />
        ) : null}
        {overviewState.status === "ok" ? (
          <div className="space-y-4">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <SummaryCard
                label={labels.totalPlays}
                value={overviewState.data.totalPlays.toLocaleString()}
              />
              <SummaryCard
                label={labels.totalPlayedSeconds}
                value={overviewState.data.totalPlayedSeconds.toLocaleString()}
              />
            </div>
            <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
              <div className="space-y-2">
                <h3 className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">
                  {labels.playsByCampaign}
                </h3>
                <DataTable
                  columns={campaignColumns}
                  rows={overviewState.data.byCampaign}
                  getRowKey={(row) => `campaign-${row.id}`}
                />
              </div>
              <div className="space-y-2">
                <h3 className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">
                  {labels.playsByScreen}
                </h3>
                <DataTable
                  columns={screenColumns}
                  rows={overviewState.data.byScreen}
                  getRowKey={(row) => `screen-${row.id}`}
                />
              </div>
              <div className="space-y-2">
                <h3 className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">
                  {labels.playsByCreative}
                </h3>
                <DataTable
                  columns={creativeColumns}
                  rows={overviewState.data.byCreative}
                  getRowKey={(row) => `creative-${row.id}`}
                />
              </div>
            </div>
          </div>
        ) : null}
      </section>

      <section className="space-y-4">
        {sectionTitle(
          labels.campaignDeliverySummary,
          `${labels.rangeLabel}: ${appliedFrom} - ${appliedTo}`,
        )}
        {campaignState.status === "loading" ? <LoadingState /> : null}
        {campaignState.status === "error" ? (
          <ErrorState message={campaignState.message} onRetry={campaignState.retry} />
        ) : null}
        {campaignState.status === "ok" ? (
          <div className="space-y-4">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <SummaryCard
                label={labels.totalPlays}
                value={campaignState.data.totalPlays.toLocaleString()}
              />
              <SummaryCard
                label={labels.totalPlayedSeconds}
                value={campaignState.data.totalPlayedSeconds.toLocaleString()}
              />
            </div>
            <DataTable
              columns={campaignColumns}
              rows={campaignState.data.campaigns}
              getRowKey={(row) => `campaign-range-${row.id}`}
            />
          </div>
        ) : null}
      </section>

      <section className="space-y-4">
        {sectionTitle(
          labels.screenDeliverySummary,
          `${labels.rangeLabel}: ${appliedFrom} - ${appliedTo}`,
        )}
        {screenState.status === "loading" ? <LoadingState /> : null}
        {screenState.status === "error" ? (
          <ErrorState message={screenState.message} onRetry={screenState.retry} />
        ) : null}
        {screenState.status === "ok" ? (
          <div className="space-y-4">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <SummaryCard
                label={labels.totalPlays}
                value={screenState.data.totalPlays.toLocaleString()}
              />
              <SummaryCard
                label={labels.totalPlayedSeconds}
                value={screenState.data.totalPlayedSeconds.toLocaleString()}
              />
            </div>
            <DataTable
              columns={screenColumns}
              rows={screenState.data.screens}
              getRowKey={(row) => `screen-range-${row.id}`}
            />
          </div>
        ) : null}
      </section>

      <section className="space-y-4">
        {sectionTitle(labels.proofOfPlayEvents)}
        {proofOfPlayState.state.status === "loading" ? <LoadingState /> : null}
        {proofOfPlayState.state.status === "error" ? (
          <ErrorState message={proofOfPlayState.state.message} onRetry={proofOfPlayState.state.retry} />
        ) : null}
        {proofOfPlayState.state.status === "ok" ? (
          <>
            <DataTable
              columns={proofOfPlayColumns}
              rows={proofOfPlayState.state.data.items}
              getRowKey={(row) => row.id}
            />
            <PaginationControls
              page={proofOfPlayState.state.data.page}
              totalPages={proofOfPlayState.state.data.totalPages}
              totalItems={proofOfPlayState.state.data.totalItems}
              pageSize={proofOfPlayState.state.data.pageSize}
              onPageChange={proofOfPlayState.setPage}
              onPageSizeChange={proofOfPlayState.setPageSize}
            />
          </>
        ) : null}
      </section>
    </ClientPageFrame>
  );
}
