"use client";

import { useEffect, useState } from "react";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { PaginationControls } from "@/components/PaginationControls";
import { PlaylistDetailPanel } from "@/components/PlaylistDetailPanel";
import { StatusBadge } from "@/components/StatusBadge";
import { TableFilters } from "@/components/TableFilters";
import { generatePlaylists, getDailyPlaylistById, getDailyPlaylistsPaged, publishPlaylist } from "@/lib/api";
import { usePagedData } from "@/lib/api/usePagedData";
import { useTranslation } from "@/lib/i18n";
import type { ApiDailyPlaylist, DailyPlaylist } from "@/lib/types";

type Feedback =
  | { kind: "success"; message: string }
  | { kind: "info"; message: string }
  | { kind: "error"; message: string };

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

export default function PlaylistsPage() {
  const { dictionary } = useTranslation();
  const labels = dictionary.pages.playlists;

  const { state, query, setPage, setPageSize, setSearch, setStatus } = usePagedData(getDailyPlaylistsPaged, "playlists");

  const [selectedDate, setSelectedDate] = useState<string>(todayIsoDate);
  const [generating, setGenerating] = useState(false);
  const [publishingId, setPublishingId] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const [detailPlaylist, setDetailPlaylist] = useState<DailyPlaylist | null>(null);

  const openDetail = async (playlist: ApiDailyPlaylist) => {
    const result = await getDailyPlaylistById(playlist.id);

    if (!result.ok) {
      setFeedback({ kind: "error", message: result.message || labels.publishError });
      return;
    }

    setDetailPlaylist(result.data);
  };

  const closeDetail = () => {
    setDetailPlaylist(null);
  };

  useEffect(() => {
    if (!feedback) {
      return;
    }
    const timeout = setTimeout(() => setFeedback(null), 5000);
    return () => clearTimeout(timeout);
  }, [feedback]);

  const handleGenerate = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedDate || generating) {
      return;
    }

    setGenerating(true);
    setFeedback(null);

    const result = await generatePlaylists(selectedDate);

    setGenerating(false);

    if (!result.ok) {
      setFeedback({ kind: "error", message: result.message || labels.generateError });
      return;
    }

    if (result.data.length === 0) {
      setFeedback({ kind: "info", message: labels.generateEmpty });
    } else {
      setFeedback({ kind: "success", message: labels.generateSuccess });
    }

    state.retry();
  };

  const handlePublish = async (playlist: ApiDailyPlaylist) => {
    if (playlist.status !== "Draft" || publishingId) {
      return;
    }

    setPublishingId(playlist.id);
    setFeedback(null);

    const result = await publishPlaylist(playlist.id);

    setPublishingId(null);

    if (!result.ok) {
      setFeedback({ kind: "error", message: result.message || labels.publishError });
      return;
    }

    setFeedback({ kind: "success", message: labels.publishSuccess });
    state.retry();
  };

  const handleDetailPublished = (updated: DailyPlaylist) => {
    setFeedback({ kind: "success", message: labels.publishSuccess });
    setDetailPlaylist(updated);
    state.retry();
  };

  const columns: TableColumn<ApiDailyPlaylist>[] = [
    { key: "date", header: labels.columnDate, render: (row) => <span className="font-mono text-xs">{row.date}</span> },
    { key: "screen", header: labels.columnScreen, render: (row) => <span className="font-mono text-xs">{row.screenId}</span> },
    { key: "version", header: labels.columnVersion, render: (row) => <span className="font-mono text-xs">v{row.version}</span> },
    { key: "status", header: labels.columnStatus, render: (row) => <StatusBadge status={row.status} /> },
    { key: "items", header: labels.columnItems, render: (row) => row.items.length },
    {
      key: "generatedAt",
      header: labels.columnGeneratedAt,
      render: (row) => <span className="font-mono text-xs">{row.generatedAt}</span>,
    },
    {
      key: "publishedAt",
      header: labels.columnPublishedAt,
      render: (row) => <span className="font-mono text-xs">{row.publishedAt ?? "-"}</span>,
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => {
        const isPublishing = publishingId === row.id;
        return (
          <div className="flex flex-wrap justify-end gap-2">
            <button
              type="button"
              onClick={() => openDetail(row)}
              className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
            >
              {labels.viewDetails}
            </button>
            {row.status === "Draft" ? (
              <button
                type="button"
                onClick={() => handlePublish(row)}
                disabled={Boolean(publishingId)}
                className="rounded-full bg-[var(--accent)] px-3 py-1 text-xs font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isPublishing ? labels.publishing : labels.publish}
              </button>
            ) : null}
          </div>
        );
      },
    },
  ];

  return (
    <ClientPageFrame section="playlists">
      <div className="panel rounded-[28px] px-5 py-4 text-sm leading-6 text-[var(--muted)]">{labels.note}</div>

      <form
        onSubmit={handleGenerate}
        className="panel flex flex-col gap-3 rounded-[28px] px-5 py-4 sm:flex-row sm:items-end sm:justify-between"
      >
        <div className="flex flex-col gap-1.5">
          <label
            htmlFor="playlist-date"
            className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]"
          >
            {labels.selectDate}
          </label>
          <input
            id="playlist-date"
            type="date"
            value={selectedDate}
            onChange={(event) => setSelectedDate(event.target.value)}
            disabled={generating}
            className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-white/5"
          />
        </div>
        <button
          type="submit"
          disabled={generating || !selectedDate}
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-60"
        >
          {generating ? labels.generating : labels.generate}
        </button>
      </form>

      <TableFilters
        search={query.search ?? ""}
        onSearchChange={setSearch}
        status={query.status}
        onStatusChange={setStatus}
        statusOptions={[
          { value: "Draft", label: "Draft" },
          { value: "Published", label: "Published" },
          { value: "Downloaded", label: "Downloaded" },
          { value: "Expired", label: "Expired" },
        ]}
      />

      {feedback ? (
        <div
          role={feedback.kind === "error" ? "alert" : "status"}
          className={`rounded-2xl border px-4 py-3 text-sm ${
            feedback.kind === "error"
              ? "border-rose-500/30 bg-rose-500/10 text-rose-700 dark:text-rose-300"
              : feedback.kind === "success"
                ? "border-emerald-500/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300"
                : "border-sky-500/30 bg-sky-500/10 text-sky-700 dark:text-sky-300"
          }`}
        >
          {feedback.message}
        </div>
      ) : null}

      {state.status === "loading" ? <LoadingState /> : null}
      {state.status === "error" ? <ErrorState message={state.message} onRetry={state.retry} /> : null}
      {state.status === "ok" ? (
        <>
          <DataTable columns={columns} rows={state.data.items} getRowKey={(row) => row.id} />
          <PaginationControls
            page={state.data.page}
            totalPages={state.data.totalPages}
            totalItems={state.data.totalItems}
            pageSize={state.data.pageSize}
            onPageChange={setPage}
            onPageSizeChange={setPageSize}
          />
        </>
      ) : null}

      {detailPlaylist ? (
        <PlaylistDetailPanel
          key={detailPlaylist.id}
          playlist={detailPlaylist}
          onClose={closeDetail}
          onPublished={handleDetailPublished}
        />
      ) : null}
    </ClientPageFrame>
  );
}
