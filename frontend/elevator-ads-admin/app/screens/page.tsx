"use client";

import { useState } from "react";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { PaginationControls } from "@/components/PaginationControls";
import { ScreenForm } from "@/components/ScreenForm";
import { StatusBadge } from "@/components/StatusBadge";
import { TableFilters } from "@/components/TableFilters";
import { getBuildingsList, getScreensPaged } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { usePagedData } from "@/lib/api/usePagedData";
import { useTranslation } from "@/lib/i18n";
import type { ApiScreen } from "@/lib/types";

export default function ScreensPage() {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;
  const page = dictionary.pages.screens;
  const { state, query, setPage, setPageSize, setSearch, setStatus } = usePagedData(getScreensPaged, "screens");
  const buildingsState = useApiData(getBuildingsList);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ApiScreen | null>(null);

  const buildingNames = new Map(
    buildingsState.status === "ok"
      ? buildingsState.data.map((building) => [building.id, building.name] as const)
      : [],
  );

  const closeModal = () => {
    setModalOpen(false);
    setEditing(null);
  };

  const openNew = () => {
    setEditing(null);
    setModalOpen(true);
  };

  const openEdit = (screen: ApiScreen) => {
    setEditing(screen);
    setModalOpen(true);
  };

  const handleSuccess = () => {
    closeModal();
    state.retry();
  };

  const columns: TableColumn<ApiScreen>[] = [
    { key: "name", header: forms.screen.name, render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "buildingId", header: forms.screen.buildingId, render: (row) => buildingNames.get(row.buildingId) ?? "—" },
    {
      key: "resolution",
      header: page.columns.resolution,
      render: (row) => <span className="font-mono text-xs">{`${row.resolutionWidth}x${row.resolutionHeight}`}</span>,
    },
    { key: "orientation", header: forms.screen.orientation, render: (row) => row.orientation },
    { key: "status", header: dictionary.forms.advertiser.status, render: (row) => <StatusBadge status={row.status} /> },
    {
      key: "lastSeen",
      header: page.columns.lastSeen,
      render: (row) => <span className="font-mono text-xs">{row.lastSeenAt ?? "-"}</span>,
    },
    {
      key: "externalCode",
      header: forms.screen.externalCode,
      render: (row) => <span className="font-mono text-xs">{row.externalCode}</span>,
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => {
        return (
          <button
            type="button"
            onClick={() => {
              openEdit(row);
            }}
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
      <TableFilters
        search={query.search ?? ""}
        onSearchChange={setSearch}
        status={query.status}
        onStatusChange={setStatus}
        statusOptions={[
          { value: "Active", label: "Active" },
          { value: "Inactive", label: "Inactive" },
          { value: "Offline", label: "Offline" },
          { value: "Maintenance", label: "Maintenance" },
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
