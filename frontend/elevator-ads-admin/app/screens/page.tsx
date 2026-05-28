"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { StatusBadge } from "@/components/StatusBadge";
import { getScreens } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import type { Screen } from "@/lib/types";

export default function ScreensPage() {
  const state = useApiData(getScreens);

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
      {state.status === "loading" ? <LoadingState /> : null}
      {state.status === "error" ? <ErrorState message={state.message} onRetry={state.retry} /> : null}
      {state.status === "ok" ? <DataTable columns={columns} rows={state.data} getRowKey={(row) => row.id} /> : null}
    </ClientPageFrame>
  );
}
