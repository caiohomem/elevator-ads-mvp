"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { StatusBadge } from "@/components/StatusBadge";
import { getBuildings } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { useTranslation } from "@/lib/i18n";
import type { Building } from "@/lib/types";

export default function BuildingsPage() {
  const { dictionary } = useTranslation();
  const state = useApiData(getBuildings);

  const columns: TableColumn<Building>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "city", header: "City", render: (row) => row.city },
    { key: "country", header: "Country", render: (row) => row.country },
    { key: "type", header: "Type", render: (row) => row.type },
    {
      key: "audience",
      header: "Estimated daily audience",
      render: (row) => row.estimatedDailyAudience.toLocaleString("en-US"),
    },
    { key: "screens", header: "Screens", render: (row) => row.screens },
    { key: "status", header: "Status", render: (row) => <StatusBadge status={row.status} /> },
  ];

  return (
    <ClientPageFrame
      section="buildings"
      action={
        <button
          type="button"
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
        >
          {dictionary.common.newBuilding}
        </button>
      }
    >
      {state.status === "loading" ? <LoadingState /> : null}
      {state.status === "error" ? <ErrorState message={state.message} onRetry={state.retry} /> : null}
      {state.status === "ok" ? <DataTable columns={columns} rows={state.data} getRowKey={(row) => row.id} /> : null}
    </ClientPageFrame>
  );
}
