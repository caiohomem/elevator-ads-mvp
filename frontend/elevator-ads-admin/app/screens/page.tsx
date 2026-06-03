"use client";

import { useState } from "react";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { ScreenForm } from "@/components/ScreenForm";
import { StatusBadge } from "@/components/StatusBadge";
import { getBuildingsList, getScreens, getScreensList } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { useTranslation } from "@/lib/i18n";
import type { ApiScreen, Screen } from "@/lib/types";

export default function ScreensPage() {
  const { dictionary } = useTranslation();
  const state = useApiData(getScreens);
  const buildingsState = useApiData(getBuildingsList);
  const rawState = useApiData(getScreensList);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ApiScreen | null>(null);

  const closeModal = () => {
    setModalOpen(false);
    setEditing(null);
  };

  const openNew = () => {
    setEditing(null);
    setModalOpen(true);
  };

  const openEdit = (raw: ApiScreen) => {
    setEditing(raw);
    setModalOpen(true);
  };

  const handleSuccess = () => {
    closeModal();
    state.retry();
    rawState.retry();
  };

  const columns: TableColumn<Screen>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "building", header: "Building", render: (row) => row.buildingName },
    {
      key: "resolution",
      header: "Resolution",
      render: (row) => <span className="font-mono text-xs">{row.resolution}</span>,
    },
    { key: "orientation", header: "Orientation", render: (row) => row.orientation },
    { key: "status", header: "Status", render: (row) => <StatusBadge status={row.status} /> },
    {
      key: "lastSeen",
      header: "Last seen",
      render: (row) => <span className="font-mono text-xs">{row.lastSeen}</span>,
    },
    {
      key: "playlist",
      header: "Current playlist",
      render: (row) => <span className="font-mono text-xs">{row.currentPlaylist}</span>,
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => {
        const raw = rawState.status === "ok" ? rawState.data.find((s) => s.id === row.id) : undefined;
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
      section="screens"
      action={
        <button
          type="button"
          onClick={openNew}
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
        >
          {dictionary.forms.newScreen}
        </button>
      }
    >
      {state.status === "loading" ? <LoadingState /> : null}
      {state.status === "error" ? <ErrorState message={state.message} onRetry={state.retry} /> : null}
      {state.status === "ok" ? <DataTable columns={columns} rows={state.data} getRowKey={(row) => row.id} /> : null}

      <Modal
        open={modalOpen}
        onClose={closeModal}
        title={editing ? dictionary.forms.editScreen : dictionary.forms.newScreen}
      >
        <ScreenForm
          initial={editing}
          buildings={buildingsState.status === "ok" ? buildingsState.data : []}
          onSuccess={handleSuccess}
          onCancel={closeModal}
        />
      </Modal>
    </ClientPageFrame>
  );
}
