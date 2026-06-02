"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { StatusBadge } from "@/components/StatusBadge";
import { getAdvertisers } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import type { Advertiser } from "@/lib/types";

export default function AdvertisersPage() {
  const state = useApiData(getAdvertisers);

  const columns: TableColumn<Advertiser>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "contact", header: "Contact", render: (row) => row.contact },
    { key: "email", header: "Email", render: (row) => row.email },
    { key: "status", header: "Status", render: (row) => <StatusBadge status={row.status} /> },
    { key: "campaigns", header: "Campaigns", render: (row) => row.campaigns },
  ];

  return (
    <ClientPageFrame section="advertisers">
      {state.status === "loading" ? <LoadingState /> : null}
      {state.status === "error" ? <ErrorState message={state.message} onRetry={state.retry} /> : null}
      {state.status === "ok" ? <DataTable columns={columns} rows={state.data} getRowKey={(row) => row.id} /> : null}
    </ClientPageFrame>
  );
}
