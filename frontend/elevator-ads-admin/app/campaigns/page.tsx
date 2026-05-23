"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { StatusBadge } from "@/components/StatusBadge";
import { campaigns } from "@/lib/mockData";
import type { Campaign } from "@/lib/types";

export default function CampaignsPage() {
  const columns: TableColumn<Campaign>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "advertiser", header: "Advertiser", render: (row) => row.advertiserName },
    { key: "status", header: "Status", render: (row) => <StatusBadge status={row.status} /> },
    {
      key: "startDate",
      header: "Start date",
      render: (row) => <span className="font-mono text-xs">{row.startDate}</span>,
    },
    {
      key: "endDate",
      header: "End date",
      render: (row) => <span className="font-mono text-xs">{row.endDate}</span>,
    },
    { key: "budget", header: "Daily budget", render: (row) => `€${row.dailyBudget}` },
    { key: "creatives", header: "Creatives", render: (row) => row.creatives },
    {
      key: "constraints",
      header: "Delivery constraints",
      render: (row) => row.deliveryConstraints,
      className: "min-w-[220px]",
    },
  ];

  return (
    <ClientPageFrame section="campaigns">
      <DataTable columns={columns} rows={campaigns} getRowKey={(row) => row.id} />
    </ClientPageFrame>
  );
}
