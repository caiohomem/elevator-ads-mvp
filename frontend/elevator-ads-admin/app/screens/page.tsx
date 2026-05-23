"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { StatusBadge } from "@/components/StatusBadge";
import { screens } from "@/lib/mockData";
import type { Screen } from "@/lib/types";

export default function ScreensPage() {
  const columns: TableColumn<Screen>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "building", header: "Building", render: (row) => row.buildingName },
    {
      key: "resolution",
      header: "Resolution",
      render: (row) => <span className="font-mono text-xs">{row.resolution}</span>,
    },
    { key: "orientation", header: "Orientation", render: (row) => row.orientation },
    { key: "status", header: "Status", render: (row) => <StatusBadge status={row.status} /> },
    {
      key: "lastSeen",
      header: "Last seen",
      render: (row) => <span className="font-mono text-xs">{row.lastSeen}</span>,
    },
    {
      key: "playlist",
      header: "Current playlist",
      render: (row) => <span className="font-mono text-xs">{row.currentPlaylist}</span>,
    },
  ];

  return (
    <ClientPageFrame section="screens">
      <DataTable columns={columns} rows={screens} getRowKey={(row) => row.id} />
    </ClientPageFrame>
  );
}
