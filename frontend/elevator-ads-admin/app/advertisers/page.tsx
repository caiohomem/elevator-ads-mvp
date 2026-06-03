"use client";

import { useState } from "react";
import { AdvertiserForm } from "@/components/AdvertiserForm";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { StatusBadge } from "@/components/StatusBadge";
import { getAdvertisers, getAdvertisersList } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { useTranslation } from "@/lib/i18n";
import type { Advertiser, ApiAdvertiser } from "@/lib/types";

export default function AdvertisersPage() {
  const { dictionary } = useTranslation();
  const state = useApiData(getAdvertisers);
  const rawState = useApiData(getAdvertisersList);
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

  const openEdit = (raw: ApiAdvertiser) => {
    setEditing(raw);
    setModalOpen(true);
  };

  const handleSuccess = () => {
    closeModal();
    state.retry();
    rawState.retry();
  };

  const columns: TableColumn<Advertiser>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "contact", header: "Contact", render: (row) => row.contact },
    { key: "email", header: "Email", render: (row) => row.email },
    { key: "status", header: "Status", render: (row) => <StatusBadge status={row.status} /> },
    { key: "campaigns", header: "Campaigns", render: (row) => row.campaigns },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => {
        const raw = rawState.status === "ok" ? rawState.data.find((a) => a.id === row.id) : undefined;
        return (
          <button
            type="button"
            onClick={() => {
              if (raw) {
                openEdit(raw);
              }
            }}
            disabled={!raw}
            className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 disabled:cursor-not-allowed disabled:opacity-60 dark:hover:bg-white/5"
          >
            {dictionary.forms.edit}
          </button>
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
      {state.status === "loading" ? <LoadingState /> : null}
      {state.status === "error" ? <ErrorState message={state.message} onRetry={state.retry} /> : null}
      {state.status === "ok" ? <DataTable columns={columns} rows={state.data} getRowKey={(row) => row.id} /> : null}

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
