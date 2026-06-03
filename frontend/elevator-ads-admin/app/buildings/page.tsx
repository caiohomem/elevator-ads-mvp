"use client";

import { useState } from "react";
import { BuildingForm } from "@/components/BuildingForm";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { PaginationControls } from "@/components/PaginationControls";
import { TableFilters } from "@/components/TableFilters";
import { getBuildingsPaged } from "@/lib/api";
import { usePagedData } from "@/lib/api/usePagedData";
import { useTranslation } from "@/lib/i18n";
import type { ApiBuilding } from "@/lib/types";

export default function BuildingsPage() {
  const { dictionary } = useTranslation();
  const { state, query, setPage, setPageSize, setSearch } = usePagedData(getBuildingsPaged, "buildings");
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

  const openEdit = (building: ApiBuilding) => {
    setEditing(building);
    setModalOpen(true);
  };

  const handleSuccess = () => {
    closeModal();
    state.retry();
  };

  const columns: TableColumn<ApiBuilding>[] = [
    { key: "name", header: "Name", render: (row) => <span className="font-semibold">{row.name}</span> },
    { key: "city", header: "City", render: (row) => row.city },
    { key: "country", header: "Country", render: (row) => row.country },
    { key: "buildingType", header: "Type", render: (row) => row.buildingType },
    {
      key: "audience",
      header: "Estimated daily audience",
      render: (row) => row.estimatedDailyAudience.toLocaleString("en-US"),
    },
    { key: "postalCode", header: "Postal code", render: (row) => row.postalCode },
    { key: "createdAt", header: "Created at", render: (row) => <span className="font-mono text-xs">{row.createdAt}</span> },
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
      <TableFilters search={query.search ?? ""} onSearchChange={setSearch} />
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
        title={editing ? dictionary.forms.editBuilding : dictionary.forms.newBuilding}
      >
        <BuildingForm initial={editing} onSuccess={handleSuccess} onCancel={closeModal} />
      </Modal>
    </ClientPageFrame>
  );
}
