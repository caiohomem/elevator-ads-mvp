"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { StatusBadge } from "@/components/StatusBadge";
import { useTranslation } from "@/lib/i18n";
import { dailyPlaylists } from "@/lib/mockData";
import type { DailyPlaylist } from "@/lib/types";

export default function PlaylistsPage() {
  const { dictionary } = useTranslation();

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
      <DataTable columns={columns} rows={dailyPlaylists} getRowKey={(row) => row.id} />
    </ClientPageFrame>
  );
}
