"use client";

import { useState } from "react";
import { BuildingForm } from "@/components/BuildingForm";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { StatusBadge } from "@/components/StatusBadge";
import { getBuildings, getBuildingsList } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { useTranslation } from "@/lib/i18n";
import type { ApiBuilding, Building } from "@/lib/types";

export default function BuildingsPage() {
  const { dictionary } = useTranslation();
  const state = useApiData(getBuildings);
  const rawState = useApiData(getBuildingsList);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ApiBuilding | null>(null);

  const closeModal = () => {
    setModalOpen(false);
    setEditing(null);
  };

  const openNew = () => {
    setEditing(null);
    setModalOpen(true);
  };

  const openEdit = (raw: ApiBuilding) => {
    setEditing(raw);
    setModalOpen(true);
  };

  const handleSuccess = () => {
    closeModal();
    state.retry();
    rawState.retry();
  };

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
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => {
        const raw = rawState.status === "ok" ? rawState.data.find((b) => b.id === row.id) : undefined;
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
      section="buildings"
      action={
        <button
          type="button"
          onClick={openNew}
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
        >
          {dictionary.forms.newBuilding}
        </button>
      }
    >
      {state.status === "loading" ? <LoadingState /> : null}
      {state.status === "error" ? <ErrorState message={state.message} onRetry={state.retry} /> : null}
      {state.status === "ok" ? <DataTable columns={columns} rows={state.data} getRowKey={(row) => row.id} /> : null}

      <Modal
        open={modalOpen}
        onClose={closeModal}
        title={editing ? dictionary.forms.editBuilding : dictionary.forms.newBuilding}
      >
        <BuildingForm initial={editing} onSuccess={handleSuccess} onCancel={closeModal} />
      </Modal>
    </ClientPageFrame>
  );
}
