"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { StatusBadge } from "@/components/StatusBadge";
import { advertisers } from "@/lib/mockData";
import type { Advertiser } from "@/lib/types";

export default function AdvertisersPage() {
  const columns: TableColumn<Advertiser>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "contact", header: "Contact", render: (row) => row.contact },
    { key: "email", header: "Email", render: (row) => row.email },
    { key: "status", header: "Status", render: (row) => <StatusBadge status={row.status} /> },
    { key: "campaigns", header: "Campaigns", render: (row) => row.campaigns },
  ];

  return (
    <ClientPageFrame section="advertisers">
      <DataTable columns={columns} rows={advertisers} getRowKey={(row) => row.id} />
    </ClientPageFrame>
  );
}
