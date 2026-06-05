"use client";

import Link from "next/link";
import { useState } from "react";
import { AdvertiserForm } from "@/components/AdvertiserForm";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { PaginationControls } from "@/components/PaginationControls";
import { StatusBadge } from "@/components/StatusBadge";
import { TableFilters } from "@/components/TableFilters";
import { getAdvertisersPaged } from "@/lib/api";
import { usePagedData } from "@/lib/api/usePagedData";
import { useTranslation } from "@/lib/i18n";
import type { ApiAdvertiser } from "@/lib/types";

export default function AdvertisersPage() {
  const { dictionary } = useTranslation();
  const { state, query, setPage, setPageSize, setSearch, setStatus } = usePagedData(getAdvertisersPaged, "advertisers");
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ApiAdvertiser | null>(null);

  const closeModal = () => {
    setModalOpen(false);
    setEditing(null);
  };

  const openNew = () => {
    setEditing(null);
    setModalOpen(true);
  };

  const openEdit = (advertiser: ApiAdvertiser) => {
    setEditing(advertiser);
    setModalOpen(true);
  };

  const handleSuccess = () => {
    closeModal();
    state.retry();
  };

  const columns: TableColumn<ApiAdvertiser>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "contact", header: "Contact", render: (row) => row.contactName },
    { key: "email", header: "Email", render: (row) => row.contactEmail },
    { key: "status", header: "Status", render: (row) => <StatusBadge status={row.status} /> },
    { key: "phone", header: "Phone", render: (row) => row.phone },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => {
        return (
          <div className="flex justify-end gap-2">
            <Link
              href={`/advertisers/${row.id}/api-keys`}
              className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
            >
              {dictionary.pages.advertiserApiKeys.shortAction}
            </Link>
            <button
              type="button"
              onClick={() => {
                openEdit(row);
              }}
              className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 disabled:cursor-not-allowed disabled:opacity-60 dark:hover:bg-white/5"
            >
              {dictionary.forms.edit}
            </button>
          </div>
        );
      },
    },
  ];

  return (
    <ClientPageFrame
      section="advertisers"
      action={
        <button
          type="button"
          onClick={openNew}
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
        >
          {dictionary.forms.newAdvertiser}
        </button>
      }
    >
      <TableFilters
        search={query.search ?? ""}
        onSearchChange={setSearch}
        status={query.status}
        onStatusChange={setStatus}
        statusOptions={[
          { value: "Active", label: "Active" },
          { value: "Inactive", label: "Inactive" },
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
        open={modalOpen}
        onClose={closeModal}
        title={editing ? dictionary.forms.editAdvertiser : dictionary.forms.newAdvertiser}
      >
        <AdvertiserForm initial={editing} onSuccess={handleSuccess} onCancel={closeModal} />
      </Modal>
    </ClientPageFrame>
  );
}
