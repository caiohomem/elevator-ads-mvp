"use client";

import { useState } from "react";
import { CampaignCreativePanel } from "@/components/CampaignCreativePanel";
import { CampaignForm } from "@/components/CampaignForm";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { StatusBadge } from "@/components/StatusBadge";
import { getAdvertisersList, getCampaigns, getCampaignsList } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { useTranslation } from "@/lib/i18n";
import type { ApiCampaign, Campaign } from "@/lib/types";

export default function CampaignsPage() {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;

  const state = useApiData(getCampaigns);
  const rawState = useApiData(getCampaignsList);
  const advertisersState = useApiData(getAdvertisersList);

  const [campaignModalOpen, setCampaignModalOpen] = useState(false);
  const [editing, setEditing] = useState<ApiCampaign | null>(null);
  const [assignModal, setAssignModal] = useState<{ campaignId: string; advertiserId: string } | null>(null);

  const closeCampaignModal = () => {
    setCampaignModalOpen(false);
    setEditing(null);
  };

  const openNewCampaign = () => {
    setEditing(null);
    setCampaignModalOpen(true);
  };

  const openEditCampaign = (raw: ApiCampaign) => {
    setEditing(raw);
    setCampaignModalOpen(true);
  };

  const handleCampaignSuccess = () => {
    closeCampaignModal();
    state.retry();
    rawState.retry();
  };

  const closeAssignModal = () => {
    setAssignModal(null);
    state.retry();
  };

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
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => {
        const raw = rawState.status === "ok" ? rawState.data.find((c) => c.id === row.id) : undefined;
        return (
          <div className="flex flex-wrap justify-end gap-2">
            <button
              type="button"
              onClick={() => {
                if (raw) {
                  setAssignModal({ campaignId: row.id, advertiserId: row.advertiserId });
                }
              }}
              disabled={!raw}
              className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 disabled:cursor-not-allowed disabled:opacity-60 dark:hover:bg-white/5"
            >
              {forms.manageCreatives}
            </button>
            <button
              type="button"
              onClick={() => {
                if (raw) {
                  openEditCampaign(raw);
                }
              }}
              disabled={!raw}
              className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 disabled:cursor-not-allowed disabled:opacity-60 dark:hover:bg-white/5"
            >
              {forms.edit}
            </button>
          </div>
        );
      },
    },
  ];

  return (
    <ClientPageFrame
      section="campaigns"
      action={
        <button
          type="button"
          onClick={openNewCampaign}
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
        >
          {forms.newCampaign}
        </button>
      }
    >
      {state.status === "loading" ? <LoadingState /> : null}
      {state.status === "error" ? <ErrorState message={state.message} onRetry={state.retry} /> : null}
      {state.status === "ok" ? <DataTable columns={columns} rows={state.data} getRowKey={(row) => row.id} /> : null}

      <Modal
        open={campaignModalOpen}
        onClose={closeCampaignModal}
        title={editing ? forms.editCampaign : forms.newCampaign}
      >
        <CampaignForm
          initial={editing}
          advertisers={advertisersState.status === "ok" ? advertisersState.data : []}
          onSuccess={handleCampaignSuccess}
          onCancel={closeCampaignModal}
        />
      </Modal>

      {assignModal ? (
        <CampaignCreativePanel
          campaignId={assignModal.campaignId}
          advertiserId={assignModal.advertiserId}
          onClose={closeAssignModal}
        />
      ) : null}
    </ClientPageFrame>
  );
}
