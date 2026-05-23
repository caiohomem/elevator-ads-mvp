"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { StatusBadge } from "@/components/StatusBadge";
import { useTranslation } from "@/lib/i18n";
import { buildings } from "@/lib/mockData";
import type { Building } from "@/lib/types";

export default function BuildingsPage() {
  const { dictionary } = useTranslation();

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
      <DataTable columns={columns} rows={buildings} getRowKey={(row) => row.id} />
    </ClientPageFrame>
  );
}
