"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { StatusBadge } from "@/components/StatusBadge";
import { getDailyPlaylists } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { useTranslation } from "@/lib/i18n";
import type { DailyPlaylist } from "@/lib/types";

export default function PlaylistsPage() {
  const { dictionary } = useTranslation();
  const state = useApiData(getDailyPlaylists);

  const columns: TableColumn<DailyPlaylist>[] = [
    { key: "date", header: "Date", render: (row) => <span className="font-mono text-xs">{row.date}</span> },
    { key: "screen", header: "Screen", render: (row) => <span className="font-semibold">{row.screenName}</span> },
    { key: "building", header: "Building", render: (row) => row.buildingName },
    { key: "version", header: "Version", render: (row) => <span className="font-mono text-xs">{row.version}</span> },
    { key: "status", header: "Status", render: (row) => <StatusBadge status={row.status} /> },
    { key: "items", header: "Items", render: (row) => row.items.length },
    {
      key: "generatedAt",
      header: "Generated at",
      render: (row) => <span className="font-mono text-xs">{row.generatedAt}</span>,
    },
    {
      key: "publishedAt",
      header: "Published at",
      render: (row) => <span className="font-mono text-xs">{row.publishedAt ?? "-"}</span>,
    },
    {
      key: "downloadedAt",
      header: "Downloaded at",
      render: (row) => <span className="font-mono text-xs">{row.downloadedAt ?? "-"}</span>,
    },
  ];

  return (
    <ClientPageFrame section="playlists">
      <div className="panel rounded-[28px] px-5 py-4 text-sm leading-6 text-[var(--muted)]">
        {dictionary.pages.playlists.note}
      </div>
      {state.status === "loading" ? <LoadingState /> : null}
      {state.status === "error" ? <ErrorState message={state.message} onRetry={state.retry} /> : null}
      {state.status === "ok" ? <DataTable columns={columns} rows={state.data} getRowKey={(row) => row.id} /> : null}
    </ClientPageFrame>
  );
}
