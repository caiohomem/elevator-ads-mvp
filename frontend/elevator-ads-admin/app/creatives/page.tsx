"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { StatusBadge } from "@/components/StatusBadge";
import { creatives } from "@/lib/mockData";
import type { Creative } from "@/lib/types";

export default function CreativesPage() {
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
      <DataTable columns={columns} rows={creatives} getRowKey={(row) => row.id} />
    </ClientPageFrame>
  );
}
