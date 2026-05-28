"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { StatusBadge } from "@/components/StatusBadge";
import { getCreatives } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import type { Creative } from "@/lib/types";

export default function CreativesPage() {
  const state = useApiData(getCreatives);

  const columns: TableColumn<Creative>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "advertiser", header: "Advertiser", render: (row) => row.advertiserName },
    { key: "mediaType", header: "Media type", render: (row) => row.mediaType },
    {
      key: "duration",
      header: "Duration",
      render: (row) => <span className="font-mono text-xs">{row.durationSeconds}s</span>,
    },
    {
      key: "approvalStatus",
      header: "Approval status",
      render: (row) => <StatusBadge status={row.approvalStatus} />,
    },
  ];

  return (
    <ClientPageFrame section="creatives">
      {state.status === "loading" ? <LoadingState /> : null}
      {state.status === "error" ? <ErrorState message={state.message} onRetry={state.retry} /> : null}
      {state.status === "ok" ? <DataTable columns={columns} rows={state.data} getRowKey={(row) => row.id} /> : null}
    </ClientPageFrame>
  );
}
