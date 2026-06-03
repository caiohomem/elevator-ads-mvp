"use client";

import { useState } from "react";
import { CampaignCreativePanel } from "@/components/CampaignCreativePanel";
import { CampaignDeliveryConstraintsPanel } from "@/components/CampaignDeliveryConstraintsPanel";
import { CampaignForm } from "@/components/CampaignForm";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { PaginationControls } from "@/components/PaginationControls";
import { StatusBadge } from "@/components/StatusBadge";
import { TableFilters } from "@/components/TableFilters";
import { getAdvertisersList, getCampaignsPaged } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { usePagedData } from "@/lib/api/usePagedData";
import { useTranslation } from "@/lib/i18n";
import type { ApiCampaign } from "@/lib/types";

export default function CampaignsPage() {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;

  const { state, query, setPage, setPageSize, setSearch, setStatus } = usePagedData(getCampaignsPaged, "campaigns");
  const advertisersState = useApiData(getAdvertisersList);

  const [campaignModalOpen, setCampaignModalOpen] = useState(false);
  const [editing, setEditing] = useState<ApiCampaign | null>(null);
  const [assignModal, setAssignModal] = useState<{ campaignId: string; advertiserId: string } | null>(null);
  const [constraintsCampaignId, setConstraintsCampaignId] = useState<string | null>(null);

  const closeCampaignModal = () => {
    setCampaignModalOpen(false);
    setEditing(null);
  };

  const openNewCampaign = () => {
    setEditing(null);
    setCampaignModalOpen(true);
  };

  const openEditCampaign = (campaign: ApiCampaign) => {
    setEditing(campaign);
    setCampaignModalOpen(true);
  };

  const handleCampaignSuccess = () => {
    closeCampaignModal();
    state.retry();
  };

  const closeAssignModal = () => {
    setAssignModal(null);
    state.retry();
  };

  const closeConstraintsModal = () => {
    setConstraintsCampaignId(null);
    state.retry();
  };

  const columns: TableColumn<ApiCampaign>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "advertiser", header: "Advertiser", render: (row) => row.advertiserId },
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
    { key: "budget", header: "Daily budget", render: (row) => (row.dailyBudget === null ? "-" : `€${row.dailyBudget}`) },
    {
      key: "totalBudget",
      header: "Total budget",
      render: (row) => (row.totalBudget === null ? "-" : `€${row.totalBudget}`),
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => {
        return (
          <div className="flex flex-wrap justify-end gap-2">
            <button
              type="button"
              onClick={() => {
                setAssignModal({ campaignId: row.id, advertiserId: row.advertiserId });
              }}
              className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 disabled:cursor-not-allowed disabled:opacity-60 dark:hover:bg-white/5"
            >
              {forms.manageCreatives}
            </button>
            <button
              type="button"
              onClick={() => {
                setConstraintsCampaignId(row.id);
              }}
              className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 disabled:cursor-not-allowed disabled:opacity-60 dark:hover:bg-white/5"
            >
              {forms.deliveryConstraints.edit}
            </button>
            <button
              type="button"
              onClick={() => {
                openEditCampaign(row);
              }}
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
      <TableFilters
        search={query.search ?? ""}
        onSearchChange={setSearch}
        status={query.status}
        onStatusChange={setStatus}
        statusOptions={[
          { value: "Draft", label: "Draft" },
          { value: "Scheduled", label: "Scheduled" },
          { value: "Active", label: "Active" },
          { value: "Paused", label: "Paused" },
        ]}
      />
      {state.status === "loading" ? <LoadingState /> : null}
      {state.status === "error" ? <ErrorState message={state.message} onRetry={state.retry} /> : null}
      {state.status === "ok" ? (
        <>
          <DataTable columns={columns} rows={state.data.items} getRowKey={(row) => row.id} />
          <PaginationControls
            page={state.data.page}
            totalPages={state.data.totalPages}
            totalItems={state.data.totalItems}
            pageSize={state.data.pageSize}
            onPageChange={setPage}
            onPageSizeChange={setPageSize}
          />
        </>
      ) : null}

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

      {constraintsCampaignId ? (
        <CampaignDeliveryConstraintsPanel
          campaignId={constraintsCampaignId}
          onClose={closeConstraintsModal}
        />
      ) : null}
    </ClientPageFrame>
  );
}
