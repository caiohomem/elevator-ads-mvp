"use client";

import { useState } from "react";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { CreativeForm } from "@/components/CreativeForm";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { PaginationControls } from "@/components/PaginationControls";
import { StatusBadge } from "@/components/StatusBadge";
import { TableFilters } from "@/components/TableFilters";
import {
  approveCreative,
  getAdvertisersList,
  getCreativesPaged,
  rejectCreative,
  submitCreativeForReview,
} from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { usePagedData } from "@/lib/api/usePagedData";
import { useTranslation } from "@/lib/i18n";
import type { ApiCreative } from "@/lib/types";

type StatusAction = "submit" | "approve" | "reject";

export default function CreativesPage() {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;
  const { state, query, setPage, setPageSize, setSearch, setStatus } = usePagedData(getCreativesPaged, "creatives");
  const advertisersState = useApiData(getAdvertisersList);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ApiCreative | null>(null);
  const [actionInFlight, setActionInFlight] = useState<{ id: string; action: StatusAction } | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const closeModal = () => {
    setModalOpen(false);
    setEditing(null);
  };

  const openNew = () => {
    setEditing(null);
    setModalOpen(true);
  };

  const openEdit = (creative: ApiCreative) => {
    setEditing(creative);
    setModalOpen(true);
  };

  const handleSuccess = () => {
    closeModal();
    state.retry();
  };

  const refresh = () => {
    state.retry();
  };

  const runStatusAction = async (id: string, action: StatusAction) => {
    setActionError(null);
    setActionInFlight({ id, action });

    const result = action === "submit"
      ? await submitCreativeForReview(id)
      : action === "approve"
        ? await approveCreative(id)
        : await rejectCreative(id);

    setActionInFlight(null);

    if (!result.ok) {
      setActionError(result.message || forms.actionFailed);
      return;
    }

    refresh();
  };

  const columns: TableColumn<ApiCreative>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "advertiser", header: "Advertiser", render: (row) => row.advertiserId },
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
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => {
        const isActing = actionInFlight?.id === row.id;
        return (
          <div className="flex flex-wrap justify-end gap-2">
            {row.approvalStatus === "Draft" ? (
              <button
                type="button"
                onClick={() => runStatusAction(row.id, "submit")}
                disabled={Boolean(actionInFlight)}
                className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 disabled:cursor-not-allowed disabled:opacity-60 dark:hover:bg-white/5"
              >
                {isActing && actionInFlight?.action === "submit" ? "..." : forms.submitForReview}
              </button>
            ) : null}
            {row.approvalStatus === "PendingReview" ? (
              <>
                <button
                  type="button"
                  onClick={() => runStatusAction(row.id, "approve")}
                  disabled={Boolean(actionInFlight)}
                  className="rounded-full border border-emerald-500/40 bg-emerald-500/10 px-3 py-1 text-xs font-semibold text-emerald-700 transition hover:bg-emerald-500/20 disabled:cursor-not-allowed disabled:opacity-60 dark:text-emerald-300"
                >
                  {isActing && actionInFlight?.action === "approve" ? "..." : forms.approve}
                </button>
                <button
                  type="button"
                  onClick={() => runStatusAction(row.id, "reject")}
                  disabled={Boolean(actionInFlight)}
                  className="rounded-full border border-rose-500/40 bg-rose-500/10 px-3 py-1 text-xs font-semibold text-rose-700 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-60 dark:text-rose-300"
                >
                  {isActing && actionInFlight?.action === "reject" ? "..." : forms.reject}
                </button>
              </>
            ) : null}
            <button
              type="button"
              onClick={() => {
                openEdit(row);
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
      section="creatives"
      action={
        <button
          type="button"
          onClick={openNew}
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
        >
          {forms.newCreative}
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
          { value: "PendingReview", label: "Pending review" },
          { value: "Approved", label: "Approved" },
          { value: "Rejected", label: "Rejected" },
        ]}
      />
      {actionError ? (
        <div
          className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-700 dark:text-rose-300"
          role="alert"
        >
          {actionError}
        </div>
      ) : null}
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
        open={modalOpen}
        onClose={closeModal}
        title={editing ? forms.editCreative : forms.newCreative}
      >
        <CreativeForm
          initial={editing}
          advertisers={advertisersState.status === "ok" ? advertisersState.data : []}
          onSuccess={handleSuccess}
          onCancel={closeModal}
        />
      </Modal>
    </ClientPageFrame>
  );
}
