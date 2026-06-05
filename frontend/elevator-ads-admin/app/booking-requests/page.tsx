"use client";

import { useEffect, useState } from "react";
import { BookingRequestForm } from "@/components/BookingRequestForm";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { PaginationControls } from "@/components/PaginationControls";
import { StatusBadge } from "@/components/StatusBadge";
import { TableFilters } from "@/components/TableFilters";
import {
  approveBookingRequest,
  generateBookingRequestForecast,
  getAdvertisersList,
  getBookingRequestForecast,
  getBookingRequestsPaged,
  rejectBookingRequest,
  submitBookingRequest,
} from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { usePagedData } from "@/lib/api/usePagedData";
import { useTranslation } from "@/lib/i18n";
import type { ApiBookingRequest, ApiCampaignForecast, BookingRequestStatus } from "@/lib/types";

type ModalMode = "create" | "edit" | "view";
type StatusAction = "submit" | "approve" | "reject";

export default function BookingRequestsPage() {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;
  const page = dictionary.pages.bookingRequests;

  const { state, query, setPage, setPageSize, setSearch, setStatus } = usePagedData(
    getBookingRequestsPaged,
    "booking-requests",
  );
  const advertisersState = useApiData(getAdvertisersList);

  const [selected, setSelected] = useState<ApiBookingRequest | null>(null);
  const [modalMode, setModalMode] = useState<ModalMode>("create");
  const [actionInFlight, setActionInFlight] = useState<{ id: string; action: StatusAction } | null>(null);
  const [actionMessage, setActionMessage] = useState<{ type: "success" | "error"; message: string } | null>(null);

  const advertiserNames = new Map(
    advertisersState.status === "ok"
      ? advertisersState.data.map((advertiser) => [advertiser.id, advertiser.name] as const)
      : [],
  );

  const closeModal = () => {
    setSelected(null);
    setModalMode("create");
  };

  const openCreate = () => {
    setActionMessage(null);
    setSelected(null);
    setModalMode("create");
  };

  const openEdit = (bookingRequest: ApiBookingRequest) => {
    setActionMessage(null);
    setSelected(bookingRequest);
    setModalMode("edit");
  };

  const openView = (bookingRequest: ApiBookingRequest) => {
    setSelected(bookingRequest);
    setModalMode("view");
  };

  const handleSuccess = () => {
    setActionMessage({ type: "success", message: page.saveSuccess });
    closeModal();
    state.retry();
  };

  const runStatusAction = async (bookingRequest: ApiBookingRequest, action: StatusAction) => {
    setActionMessage(null);
    setActionInFlight({ id: bookingRequest.id, action });

    const result = action === "submit"
      ? await submitBookingRequest(bookingRequest.id)
      : action === "approve"
        ? await approveBookingRequest(bookingRequest.id)
        : await rejectBookingRequest(bookingRequest.id);

    setActionInFlight(null);

    if (!result.ok) {
      setActionMessage({ type: "error", message: result.message || forms.actionFailed });
      return;
    }

    setActionMessage({
      type: "success",
      message:
        action === "submit"
          ? page.submitSuccess
          : action === "approve"
            ? page.approveSuccess
            : page.rejectSuccess,
    });
    state.retry();
  };

  const columns: TableColumn<ApiBookingRequest>[] = [
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
      key: "advertiser",
      header: page.columns.advertiser,
      render: (row) => advertiserNames.get(row.advertiserId) ?? row.advertiserId,
    },
    {
      key: "window",
      header: page.columns.flight,
      render: (row) => (
        <div className="space-y-1 text-xs text-[var(--muted)]">
          <div>{formatDate(row.dateFrom)}</div>
          <div>{formatDate(row.dateTo)}</div>
        </div>
      ),
    },
    {
      key: "status",
      header: page.columns.status,
      render: (row) => <StatusBadge status={row.status} label={getStatusLabel(row.status, page.statuses)} />,
    },
    {
      key: "budget",
      header: page.columns.budget,
      render: (row) => formatCurrency(row.budget),
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
      render: (row) => {
        const isActing = actionInFlight?.id === row.id;

        return (
          <div className="flex flex-wrap justify-end gap-2">
            <button
              type="button"
              onClick={() => openView(row)}
              className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
            >
              {page.view}
            </button>
            {row.status === "Draft" ? (
              <>
                <button
                  type="button"
                  onClick={() => openEdit(row)}
                  className="rounded-full border border-[var(--panel-border)] px-3 py-1 text-xs font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
                >
                  {forms.edit}
                </button>
                <button
                  type="button"
                  onClick={() => runStatusAction(row, "submit")}
                  disabled={Boolean(actionInFlight)}
                  className="rounded-full border border-sky-500/40 bg-sky-500/10 px-3 py-1 text-xs font-semibold text-sky-700 transition hover:bg-sky-500/20 disabled:cursor-not-allowed disabled:opacity-60 dark:text-sky-300"
                >
                  {isActing && actionInFlight?.action === "submit" ? "..." : forms.submit}
                </button>
              </>
            ) : null}
            {(row.status === "Submitted" || row.status === "UnderReview") ? (
              <>
                <button
                  type="button"
                  onClick={() => runStatusAction(row, "approve")}
                  disabled={Boolean(actionInFlight)}
                  className="rounded-full border border-emerald-500/40 bg-emerald-500/10 px-3 py-1 text-xs font-semibold text-emerald-700 transition hover:bg-emerald-500/20 disabled:cursor-not-allowed disabled:opacity-60 dark:text-emerald-300"
                >
                  {isActing && actionInFlight?.action === "approve" ? "..." : forms.approve}
                </button>
                <button
                  type="button"
                  onClick={() => runStatusAction(row, "reject")}
                  disabled={Boolean(actionInFlight)}
                  className="rounded-full border border-rose-500/40 bg-rose-500/10 px-3 py-1 text-xs font-semibold text-rose-700 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-60 dark:text-rose-300"
                >
                  {isActing && actionInFlight?.action === "reject" ? "..." : forms.reject}
                </button>
              </>
            ) : null}
          </div>
        );
      },
    },
  ];

  return (
    <ClientPageFrame
      section="bookingRequests"
      action={
        <button
          type="button"
          onClick={openCreate}
          className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
        >
          {forms.newBookingRequest}
        </button>
      }
    >
      <TableFilters
        search={query.search ?? ""}
        onSearchChange={setSearch}
        status={query.status}
        onStatusChange={setStatus}
        statusOptions={[
          { value: "Draft", label: getStatusLabel("Draft", page.statuses) },
          { value: "Submitted", label: getStatusLabel("Submitted", page.statuses) },
          { value: "UnderReview", label: getStatusLabel("UnderReview", page.statuses) },
          { value: "Approved", label: getStatusLabel("Approved", page.statuses) },
          { value: "Rejected", label: getStatusLabel("Rejected", page.statuses) },
          { value: "ConvertedToCampaign", label: getStatusLabel("ConvertedToCampaign", page.statuses) },
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
        title={modalMode === "edit" ? forms.editBookingRequest : forms.newBookingRequest}
      >
        <BookingRequestForm
          mode={modalMode === "edit" ? "edit" : "create"}
          initial={selected}
          advertisers={advertisersState.status === "ok" ? advertisersState.data : []}
          onSuccess={handleSuccess}
          onCancel={closeModal}
        />
      </Modal>

      <Modal open={modalMode === "view" && Boolean(selected)} onClose={closeModal} title={page.detailTitle}>
        {selected ? (
          <BookingRequestDetail
            bookingRequest={selected}
            advertiserName={advertiserNames.get(selected.advertiserId) ?? selected.advertiserId}
            statuses={page.statuses}
            labels={page.details}
            forecastLabels={page.forecast}
          />
        ) : null}
      </Modal>
    </ClientPageFrame>
  );
}

function BookingRequestDetail({
  bookingRequest,
  advertiserName,
  statuses,
  labels,
  forecastLabels,
}: {
  bookingRequest: ApiBookingRequest;
  advertiserName: string;
  statuses: Record<BookingRequestStatus, string>;
  labels: {
    advertiser: string;
    dateFrom: string;
    dateTo: string;
    cities: string;
    buildingTypes: string;
    screenOrientations: string;
    creativeDurationSeconds: string;
    budget: string;
    campaignObjective: string;
    notes: string;
  };
  forecastLabels: {
    title: string;
    generate: string;
    generating: string;
    empty: string;
    loadError: string;
    eligibleScreens: string;
    eligibleBuildings: string;
    estimatedPlays: string;
    estimatedAudience: string;
    estimatedCost: string;
    availableCapacity: string;
    warnings: string;
    conflicts: string;
    disclaimer: string;
    updatedAt: string;
  };
}) {
  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold text-[var(--foreground)]">{bookingRequest.name}</h2>
          <p className="text-sm text-[var(--muted)]">{advertiserName}</p>
        </div>
        <StatusBadge status={bookingRequest.status} label={getStatusLabel(bookingRequest.status, statuses)} />
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <DetailItem label={labels.advertiser} value={advertiserName} />
        <DetailItem label={labels.creativeDurationSeconds} value={`${bookingRequest.creativeDurationSeconds}s`} />
        <DetailItem label={labels.dateFrom} value={formatDate(bookingRequest.dateFrom)} />
        <DetailItem label={labels.dateTo} value={formatDate(bookingRequest.dateTo)} />
        <DetailItem label={labels.budget} value={formatCurrency(bookingRequest.budget)} />
        <DetailItem label={labels.campaignObjective} value={bookingRequest.campaignObjective || "—"} />
      </div>

      <DetailBlock label={labels.cities} value={bookingRequest.cities.join(", ") || "—"} />
      <DetailBlock label={labels.buildingTypes} value={bookingRequest.buildingTypes.join(", ") || "—"} />
      <DetailBlock label={labels.screenOrientations} value={bookingRequest.screenOrientations.join(", ") || "—"} />
      <DetailBlock label={labels.notes} value={bookingRequest.notes || "—"} />
      <BookingRequestForecastPanel bookingRequestId={bookingRequest.id} labels={forecastLabels} />
    </div>
  );
}

function BookingRequestForecastPanel({
  bookingRequestId,
  labels,
}: {
  bookingRequestId: string;
  labels: {
    title: string;
    generate: string;
    generating: string;
    empty: string;
    loadError: string;
    eligibleScreens: string;
    eligibleBuildings: string;
    estimatedPlays: string;
    estimatedAudience: string;
    estimatedCost: string;
    availableCapacity: string;
    warnings: string;
    conflicts: string;
    disclaimer: string;
    updatedAt: string;
  };
}) {
  const [forecast, setForecast] = useState<ApiCampaignForecast | null>(null);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadForecast = async () => {
    setLoading(true);
    setError(null);
    setForecast(null);

    const result = await getBookingRequestForecast(bookingRequestId);

    if (result.ok) {
      setForecast(result.data);
      setLoading(false);
      return;
    }

    if (result.status === 404) {
      setForecast(null);
      setLoading(false);
      return;
    }

    setError(result.message || labels.loadError);
    setLoading(false);
  };

  useEffect(() => {
    let active = true;

    queueMicrotask(() => {
      setLoading(true);
      setError(null);
      setForecast(null);

      getBookingRequestForecast(bookingRequestId)
        .then((result) => {
          if (!active) {
            return;
          }

          if (result.ok) {
            setForecast(result.data);
            return;
          }

          if (result.status === 404) {
            setForecast(null);
            return;
          }

          setError(result.message || labels.loadError);
        })
        .catch((caughtError) => {
          if (!active) {
            return;
          }

          setError(caughtError instanceof Error ? caughtError.message : labels.loadError);
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
  }, [bookingRequestId, labels.loadError]);

  const handleGenerate = async () => {
    setGenerating(true);
    setError(null);

    const result = await generateBookingRequestForecast(bookingRequestId);

    setGenerating(false);

    if (!result.ok) {
      setError(result.message || labels.loadError);
      return;
    }

    setForecast(result.data);
  };

  return (
    <section className="rounded-[28px] border border-[var(--panel-border)] bg-white/50 p-5 shadow-[0_20px_60px_rgba(15,23,42,0.08)] dark:bg-white/[0.04]">
      <div className="flex flex-col gap-3 border-b border-[var(--panel-border)] pb-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="text-lg font-semibold text-[var(--foreground)]">{labels.title}</h3>
          <p className="mt-1 text-sm text-[var(--muted)]">{labels.disclaimer}</p>
        </div>
        <button
          type="button"
          onClick={handleGenerate}
          disabled={generating}
          className="rounded-full border border-sky-500/40 bg-sky-500/10 px-4 py-2 text-sm font-semibold text-sky-700 transition hover:bg-sky-500/20 disabled:cursor-not-allowed disabled:opacity-60 dark:text-sky-300"
        >
          {generating ? labels.generating : labels.generate}
        </button>
      </div>

      {loading ? (
        <div className="pt-4">
          <LoadingState />
        </div>
      ) : null}

      {!loading && error ? (
        <div className="pt-4">
          <ErrorState message={error} onRetry={loadForecast} />
        </div>
      ) : null}

      {!loading && !error && !forecast ? (
        <div className="pt-4 text-sm text-[var(--muted)]">{labels.empty}</div>
      ) : null}

      {!loading && !error && forecast ? (
        <div className="space-y-4 pt-4">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
            <DetailItem label={labels.eligibleScreens} value={formatNumber(forecast.eligibleScreens)} />
            <DetailItem label={labels.eligibleBuildings} value={formatNumber(forecast.eligibleBuildings)} />
            <DetailItem label={labels.estimatedPlays} value={formatNumber(forecast.estimatedPlays)} />
            <DetailItem label={labels.estimatedAudience} value={formatNumber(forecast.estimatedAudience)} />
            <DetailItem label={labels.estimatedCost} value={formatCurrency(forecast.estimatedCost)} />
            <DetailItem label={labels.availableCapacity} value={formatPercent(forecast.availableCapacity)} />
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <StringListCard label={labels.warnings} items={forecast.warnings} />
            <StringListCard label={labels.conflicts} items={forecast.conflicts} />
          </div>

          <p className="text-xs text-[var(--muted)]">
            {labels.updatedAt}: {formatDateTime(forecast.updatedAt)}
          </p>
        </div>
      ) : null}
    </section>
  );
}

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--panel-border)] bg-white/40 px-4 py-3 dark:bg-white/[0.03]">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">{label}</p>
      <p className="mt-2 text-sm text-[var(--foreground)]">{value}</p>
    </div>
  );
}

function StringListCard({ label, items }: { label: string; items: string[] }) {
  return (
    <div className="rounded-2xl border border-[var(--panel-border)] bg-white/40 px-4 py-3 dark:bg-white/[0.03]">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">{label}</p>
      {items.length > 0 ? (
        <ul className="mt-2 space-y-2 text-sm leading-6 text-[var(--foreground)]">
          {items.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      ) : (
        <p className="mt-2 text-sm text-[var(--muted)]">—</p>
      )}
    </div>
  );
}

function DetailBlock({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--panel-border)] bg-white/40 px-4 py-3 dark:bg-white/[0.03]">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">{label}</p>
      <p className="mt-2 text-sm leading-6 text-[var(--foreground)]">{value}</p>
    </div>
  );
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString();
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "EUR" }).format(value);
}

function formatNumber(value: number) {
  return new Intl.NumberFormat().format(value);
}

function formatPercent(value: number) {
  return new Intl.NumberFormat(undefined, { style: "percent", maximumFractionDigits: 0 }).format(value);
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString();
}

function getStatusLabel(
  status: BookingRequestStatus,
  labels: Record<BookingRequestStatus, string>,
) {
  return labels[status] ?? status;
}
