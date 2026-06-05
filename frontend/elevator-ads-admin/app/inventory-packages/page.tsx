"use client";

import { useEffect, useState } from "react";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { InventoryPackageForm } from "@/components/InventoryPackageForm";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { PaginationControls } from "@/components/PaginationControls";
import { StatusBadge } from "@/components/StatusBadge";
import { TableFilters } from "@/components/TableFilters";
import {
  deleteInventoryPackage,
  getBuildingsList,
  getInventoryPackageScreens,
  getInventoryPackagesPaged,
  getScreensList,
} from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { usePagedData } from "@/lib/api/usePagedData";
import { useTranslation } from "@/lib/i18n";
import type { ApiInventoryPackage, ApiScreen, InventoryPackageStatus } from "@/lib/types";

type ModalMode = "closed" | "create" | "edit" | "view";

export default function InventoryPackagesPage() {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;
  const page = dictionary.pages.inventoryPackages;

  const { state, query, setPage, setPageSize, setSearch, setStatus } = usePagedData(
    getInventoryPackagesPaged,
    "inventory-packages",
  );
  const buildingsState = useApiData(getBuildingsList);
  const screensState = useApiData(getScreensList);

  const [selected, setSelected] = useState<ApiInventoryPackage | null>(null);
  const [modalMode, setModalMode] = useState<ModalMode>("closed");
  const [actionMessage, setActionMessage] = useState<{ type: "success" | "error"; message: string } | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const buildingNames = new Map(
    buildingsState.status === "ok"
      ? buildingsState.data.map((building) => [building.id, building.name] as const)
      : [],
  );

  const closeModal = () => {
    setSelected(null);
    setModalMode("closed");
  };

  const openCreate = () => {
    setActionMessage(null);
    setSelected(null);
    setModalMode("create");
  };

  const openEdit = (inventoryPackage: ApiInventoryPackage) => {
    setActionMessage(null);
    setSelected(inventoryPackage);
    setModalMode("edit");
  };

  const openView = (inventoryPackage: ApiInventoryPackage) => {
    setSelected(inventoryPackage);
    setModalMode("view");
  };

  const handleSuccess = () => {
    setActionMessage({ type: "success", message: page.saveSuccess });
    closeModal();
    state.retry();
  };

  const handleDelete = async (inventoryPackage: ApiInventoryPackage) => {
    if (typeof window !== "undefined" && !window.confirm(`${page.detailTitle}: ${inventoryPackage.name}?`)) {
      return;
    }

    setActionMessage(null);
    setDeletingId(inventoryPackage.id);

    const result = await deleteInventoryPackage(inventoryPackage.id);

    setDeletingId(null);

    if (!result.ok) {
      setActionMessage({ type: "error", message: result.message || page.deleteFailed });
      return;
    }

    if (selected?.id === inventoryPackage.id) {
      closeModal();
    }

    setActionMessage({ type: "success", message: page.deleteSuccess });
    state.retry();
  };

  const columns: TableColumn<ApiInventoryPackage>[] = [
    {
      key: "name",
      header: page.columns.name,
      render: (row) => (
        <button
          type="button"
          onClick={() => openView(row)}
          className="text-left font-semibold text-[var(--foreground)] underline-offset-4 hover:underline"
        >
          {row.name}
        </button>
      ),
    },
    {
      key: "status",
      header: page.columns.status,
      render: (row) => <StatusBadge status={row.status} label={page.statuses[row.status]} />,
    },
    {
      key: "baseCpm",
      header: page.columns.baseCpm,
      render: (row) => formatCurrency(row.baseCpm),
    },
    {
      key: "cities",
      header: page.columns.cities,
      render: (row) => row.cities.join(", ") || "—",
    },
    {
      key: "createdAt",
      header: page.columns.createdAt,
      render: (row) => <span className="font-mono text-xs">{formatDate(row.createdAt)}</span>,
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => (
        <div className="flex flex-wrap justify-end gap-2">
          <button
            type="button"
            onClick={() => openView(row)}
            className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
          >
            {page.viewScreens}
          </button>
          <button
            type="button"
            onClick={() => openEdit(row)}
            className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
          >
            {forms.edit}
          </button>
          <button
            type="button"
            onClick={() => handleDelete(row)}
            disabled={Boolean(deletingId)}
            className="rounded-full border border-rose-500/40 bg-rose-500/10 px-3 py-1 text-xs font-semibold text-rose-700 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-60 dark:text-rose-300"
          >
            {deletingId === row.id ? "..." : forms.remove}
          </button>
        </div>
      ),
    },
  ];

  return (
    <ClientPageFrame
      section="inventoryPackages"
      action={
        <button
          type="button"
          onClick={openCreate}
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
        >
          {forms.newInventoryPackage}
        </button>
      }
    >
      <TableFilters
        search={query.search ?? ""}
        onSearchChange={setSearch}
        status={query.status}
        onStatusChange={setStatus}
        statusOptions={[
          { value: "Active", label: page.statuses.Active },
          { value: "Inactive", label: page.statuses.Inactive },
        ]}
      />

      {actionMessage ? (
        <div
          className={`rounded-2xl px-4 py-3 text-sm ${
            actionMessage.type === "success"
              ? "border border-emerald-500/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300"
              : "border border-rose-500/30 bg-rose-500/10 text-rose-700 dark:text-rose-300"
          }`}
          role="status"
        >
          {actionMessage.message}
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
        open={modalMode === "create" || modalMode === "edit"}
        onClose={closeModal}
        title={modalMode === "edit" ? forms.editInventoryPackage : forms.newInventoryPackage}
      >
        <InventoryPackageForm
          mode={modalMode === "edit" ? "edit" : "create"}
          initial={selected}
          buildings={buildingsState.status === "ok" ? buildingsState.data : []}
          screens={screensState.status === "ok" ? screensState.data : []}
          onSuccess={handleSuccess}
          onCancel={closeModal}
        />
      </Modal>

      <Modal open={modalMode === "view" && Boolean(selected)} onClose={closeModal} title={page.detailTitle}>
        {selected ? (
          <InventoryPackageDetail
            inventoryPackage={selected}
            buildingNames={buildingNames}
            onDelete={handleDelete}
            deleteLabel={forms.remove}
            labels={page.details}
            statuses={page.statuses}
            screensLabels={page.screens}
            pageLabels={page}
          />
        ) : null}
      </Modal>
    </ClientPageFrame>
  );
}

function InventoryPackageDetail({
  inventoryPackage,
  buildingNames,
  onDelete,
  deleteLabel,
  labels,
  statuses,
  screensLabels,
  pageLabels,
}: {
  inventoryPackage: ApiInventoryPackage;
  buildingNames: Map<string, string>;
  onDelete: (inventoryPackage: ApiInventoryPackage) => Promise<void>;
  deleteLabel: string;
  labels: {
    description: string;
    cities: string;
    buildingTypes: string;
    screenOrientations: string;
    buildingIds: string;
    screenIds: string;
    baseCpm: string;
    updatedAt: string;
  };
  statuses: Record<InventoryPackageStatus, string>;
  screensLabels: {
    name: string;
    building: string;
    orientation: string;
    status: string;
  };
  pageLabels: {
    view: string;
    matchingScreensTitle: string;
    matchingScreensEmpty: string;
    matchingScreensLoadError: string;
    deleteFailed: string;
  };
}) {
  const [screens, setScreens] = useState<ApiScreen[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    queueMicrotask(() => {
      setLoading(true);
      setError(null);

      getInventoryPackageScreens(inventoryPackage.id)
        .then((result) => {
          if (!active) {
            return;
          }

          if (!result.ok) {
            setError(result.message || pageLabels.matchingScreensLoadError);
            return;
          }

          setScreens(result.data);
        })
        .catch((caughtError) => {
          if (!active) {
            return;
          }

          setError(caughtError instanceof Error ? caughtError.message : pageLabels.matchingScreensLoadError);
        })
        .finally(() => {
          if (active) {
            setLoading(false);
          }
        });
    });

    return () => {
      active = false;
    };
  }, [inventoryPackage.id, pageLabels.matchingScreensLoadError]);

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold text-[var(--foreground)]">{inventoryPackage.name}</h2>
          <p className="text-sm text-[var(--muted)]">{pageLabels.view}</p>
        </div>
        <div className="flex items-center gap-2">
          <StatusBadge status={inventoryPackage.status} label={statuses[inventoryPackage.status]} />
          <button
            type="button"
            onClick={() => onDelete(inventoryPackage).catch(() => null)}
            className="rounded-full border border-rose-500/40 bg-rose-500/10 px-3 py-1 text-xs font-semibold text-rose-700 transition hover:bg-rose-500/20 dark:text-rose-300"
          >
            {deleteLabel}
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <DetailItem label={labels.baseCpm} value={formatCurrency(inventoryPackage.baseCpm)} />
        <DetailItem label={labels.updatedAt} value={formatDate(inventoryPackage.updatedAt)} />
      </div>

      <DetailBlock label={labels.description} value={inventoryPackage.description || "—"} />
      <DetailBlock label={labels.cities} value={inventoryPackage.cities.join(", ") || "—"} />
      <DetailBlock label={labels.buildingTypes} value={inventoryPackage.buildingTypes.join(", ") || "—"} />
      <DetailBlock
        label={labels.screenOrientations}
        value={inventoryPackage.screenOrientations.join(", ") || "—"}
      />
      <DetailBlock
        label={labels.buildingIds}
        value={inventoryPackage.buildingIds.map((id) => buildingNames.get(id) ?? id).join(", ") || "—"}
      />
      <DetailBlock label={labels.screenIds} value={inventoryPackage.screenIds.join(", ") || "—"} />

      <section className="rounded-[28px] border border-[var(--panel-border)] bg-white/50 p-5 shadow-[0_20px_60px_rgba(15,23,42,0.08)] dark:bg-white/[0.04]">
        <h3 className="text-lg font-semibold text-[var(--foreground)]">{pageLabels.matchingScreensTitle}</h3>

        {loading ? (
          <div className="pt-4">
            <LoadingState />
          </div>
        ) : null}

        {!loading && error ? (
          <div className="pt-4">
            <ErrorState
              message={error || pageLabels.matchingScreensLoadError}
              onRetry={() => {
                setLoading(true);
                setError(null);
                getInventoryPackageScreens(inventoryPackage.id)
                  .then((result) => {
                    if (!result.ok) {
                      setError(result.message || pageLabels.matchingScreensLoadError);
                      return;
                    }

                    setScreens(result.data);
                  })
                  .catch((caughtError) => {
                    setError(
                      caughtError instanceof Error ? caughtError.message : pageLabels.matchingScreensLoadError,
                    );
                  })
                  .finally(() => {
                    setLoading(false);
                  });
              }}
            />
          </div>
        ) : null}

        {!loading && !error && !screens.length ? (
          <p className="pt-4 text-sm text-[var(--muted)]">{pageLabels.matchingScreensEmpty}</p>
        ) : null}

        {!loading && !error && screens.length ? (
          <div className="pt-4">
            <DataTable
              columns={[
                {
                  key: "name",
                  header: screensLabels.name,
                  render: (row: ApiScreen) => row.name,
                },
                {
                  key: "building",
                  header: screensLabels.building,
                  render: (row: ApiScreen) => buildingNames.get(row.buildingId) ?? row.buildingId,
                },
                {
                  key: "orientation",
                  header: screensLabels.orientation,
                  render: (row: ApiScreen) => row.orientation,
                },
                {
                  key: "status",
                  header: screensLabels.status,
                  render: (row: ApiScreen) => <StatusBadge status={row.status} />,
                },
              ]}
              rows={screens}
              getRowKey={(row) => row.id}
            />
          </div>
        ) : null}
      </section>
    </div>
  );
}

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--panel-border)] bg-white/40 px-4 py-3 dark:bg-white/[0.03]">
      <div className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">{label}</div>
      <div className="mt-2 text-sm text-[var(--foreground)]">{value}</div>
    </div>
  );
}

function DetailBlock({ label, value }: { label: string; value: string }) {
  return (
    <div className="space-y-1">
      <div className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">{label}</div>
      <div className="rounded-2xl border border-[var(--panel-border)] bg-white/40 px-4 py-3 text-sm text-[var(--foreground)] dark:bg-white/[0.03]">
        {value}
      </div>
    </div>
  );
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString();
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 2,
  }).format(value);
}
