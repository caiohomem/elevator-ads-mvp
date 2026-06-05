"use client";

import Link from "next/link";
import { useCallback, useState } from "react";
import { useParams } from "next/navigation";
import { AdvertiserApiKeyForm } from "@/components/AdvertiserApiKeyForm";
import { ClientPageFrame } from "@/components/ClientPageFrame";
import { DataTable, type TableColumn } from "@/components/DataTable";
import { ErrorState } from "@/components/ErrorState";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import { StatusBadge } from "@/components/StatusBadge";
import { getAdvertiserApiKeys, getAdvertiserById, revokeAdvertiserApiKey } from "@/lib/api";
import { useApiData } from "@/lib/api/useApiData";
import { useTranslation } from "@/lib/i18n";
import type { ApiAdvertiser, ApiAdvertiserApiKey, CreateApiKeyResponse } from "@/lib/types";

export default function AdvertiserApiKeysPage() {
  const params = useParams<{ id: string }>();
  const advertiserId = Array.isArray(params.id) ? params.id[0] : params.id;
  const { dictionary } = useTranslation();
  const page = dictionary.pages.advertiserApiKeys;
  const [createOpen, setCreateOpen] = useState(false);
  const [createdKey, setCreatedKey] = useState<CreateApiKeyResponse | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [revokingId, setRevokingId] = useState<string | null>(null);
  const [copyState, setCopyState] = useState<"idle" | "copied" | "error">("idle");

  const loadAdvertiser = useCallback(() => getAdvertiserById(advertiserId), [advertiserId]);
  const loadApiKeys = useCallback(() => getAdvertiserApiKeys(advertiserId), [advertiserId]);
  const advertiserState = useApiData(loadAdvertiser);
  const apiKeysState = useApiData(loadApiKeys);

  const advertiser = advertiserState.status === "ok" ? advertiserState.data : null;
  const keys = apiKeysState.status === "ok" ? apiKeysState.data : [];

  const columns: TableColumn<ApiAdvertiserApiKey>[] = [
    {
      key: "name",
      header: page.columns.name,
      render: (row) => (
        <div>
          <div className="font-semibold">{row.name}</div>
          <div className="font-mono text-xs text-[var(--muted)]">{row.keyPrefix}</div>
        </div>
      ),
    },
    {
      key: "scopes",
      header: page.columns.scopes,
      render: (row) => (
        <div className="flex flex-wrap gap-2">
          {row.scopes.map((scope) => (
            <span
              key={scope}
              className="rounded-full border border-[var(--panel-border)] bg-[var(--panel-strong)] px-2.5 py-1 font-mono text-xs text-[var(--muted)]"
            >
              {dictionary.forms.advertiserApiKey.scopeLabels[scope as keyof typeof dictionary.forms.advertiserApiKey.scopeLabels] ?? scope}
            </span>
          ))}
        </div>
      ),
    },
    {
      key: "status",
      header: page.columns.status,
      render: (row) => <StatusBadge status={row.status} label={page.statuses[row.status]} />,
    },
    {
      key: "lastUsedAt",
      header: page.columns.lastUsedAt,
      render: (row) => formatDateTime(row.lastUsedAt, page.notUsedYet),
    },
    {
      key: "expiresAt",
      header: page.columns.expiresAt,
      render: (row) => formatDateTime(row.expiresAt, page.noExpiration),
    },
    {
      key: "createdAt",
      header: page.columns.createdAt,
      render: (row) => formatDateTime(row.createdAt),
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => (
        row.status === "Revoked" ? null : (
          <button
            type="button"
            onClick={() => handleRevoke(row)}
            disabled={revokingId === row.id}
            className="rounded-full border border-rose-500/30 px-3 py-1 text-xs font-semibold text-rose-700 transition hover:bg-rose-500/10 disabled:cursor-not-allowed disabled:opacity-60 dark:text-rose-300"
          >
            {revokingId === row.id ? page.revoking : page.revokeButton}
          </button>
        )
      ),
    },
  ];

  async function handleRevoke(apiKey: ApiAdvertiserApiKey) {
    setActionError(null);
    setRevokingId(apiKey.id);
    const result = await revokeAdvertiserApiKey(advertiserId, apiKey.id);
    setRevokingId(null);

    if (!result.ok) {
      setActionError(result.message || dictionary.forms.actionFailed);
      return;
    }

    apiKeysState.retry();
  }

  async function handleCopyKey() {
    if (!createdKey) {
      return;
    }

    try {
      await navigator.clipboard.writeText(createdKey.plainApiKey);
      setCopyState("copied");
    } catch {
      setCopyState("error");
    }
  }

  const handleCreateSuccess = (result: CreateApiKeyResponse) => {
    setCreateOpen(false);
    setCreatedKey(result);
    setCopyState("idle");
    apiKeysState.retry();
  };

  return (
    <ClientPageFrame
      section="advertiserApiKeys"
      action={
        <div className="flex flex-wrap gap-2">
          <Link
            href="/advertisers"
            className="rounded-2xl border border-[var(--panel-border)] px-4 py-2.5 text-sm font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
          >
            {page.backToAdvertisers}
          </Link>
          <button
            type="button"
            onClick={() => setCreateOpen(true)}
            className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
          >
            {page.createButton}
          </button>
        </div>
      }
    >
      {advertiserState.status === "loading" ? <LoadingState /> : null}
      {advertiserState.status === "error" ? <ErrorState message={advertiserState.message} onRetry={advertiserState.retry} /> : null}
      {advertiser ? <AdvertiserSummary advertiser={advertiser} /> : null}

      {actionError ? (
        <div className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-700 dark:text-rose-300">
          {actionError}
        </div>
      ) : null}

      {apiKeysState.status === "loading" ? <LoadingState /> : null}
      {apiKeysState.status === "error" ? <ErrorState message={apiKeysState.message} onRetry={apiKeysState.retry} /> : null}
      {apiKeysState.status === "ok" ? (
        keys.length > 0 ? (
          <DataTable columns={columns} rows={keys} getRowKey={(row) => row.id} />
        ) : (
          <div className="rounded-[28px] border border-dashed border-[var(--panel-border)] px-6 py-12 text-center">
            <h2 className="text-lg font-semibold text-[var(--foreground)]">{page.emptyTitle}</h2>
            <p className="mt-2 text-sm text-[var(--muted)]">{page.emptyDescription}</p>
          </div>
        )
      ) : null}

      <Modal open={createOpen} onClose={() => setCreateOpen(false)} title={page.createModalTitle}>
        <AdvertiserApiKeyForm advertiserId={advertiserId} onSuccess={handleCreateSuccess} onCancel={() => setCreateOpen(false)} />
      </Modal>

      <Modal open={createdKey !== null} onClose={() => setCreatedKey(null)} title={page.createdModalTitle}>
        {createdKey ? (
          <div className="space-y-4">
            <div className="rounded-2xl border border-amber-500/25 bg-amber-500/10 px-4 py-3 text-sm text-amber-800 dark:text-amber-200">
              {page.shownOnceWarning}
            </div>
            <div className="rounded-2xl border border-[var(--panel-border)] bg-[var(--panel-strong)] p-4">
              <div className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">{page.plainKeyLabel}</div>
              <div className="mt-2 break-all font-mono text-sm text-[var(--foreground)]">{createdKey.plainApiKey}</div>
            </div>
            <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={() => setCreatedKey(null)}
                className="rounded-2xl border border-[var(--panel-border)] px-4 py-2.5 text-sm font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
              >
                {dictionary.common.close}
              </button>
              <button
                type="button"
                onClick={handleCopyKey}
                className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
              >
                {copyState === "copied" ? page.copySuccess : page.copyButton}
              </button>
            </div>
            {copyState === "error" ? (
              <div className="text-sm text-rose-700 dark:text-rose-300">{page.copyFailed}</div>
            ) : null}
          </div>
        ) : null}
      </Modal>
    </ClientPageFrame>
  );
}

function AdvertiserSummary({ advertiser }: { advertiser: ApiAdvertiser }) {
  const { dictionary } = useTranslation();
  const page = dictionary.pages.advertiserApiKeys;

  return (
    <div className="panel rounded-[28px] p-5">
      <div className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">{page.advertiserLabel}</div>
      <div className="mt-2 text-xl font-semibold text-[var(--foreground)]">{advertiser.name}</div>
      <div className="mt-1 text-sm text-[var(--muted)]">{advertiser.contactEmail}</div>
    </div>
  );
}

function formatDateTime(value: string | null, fallback = "—") {
  if (!value) {
    return <span className="text-[var(--muted)]">{fallback}</span>;
  }

  return <span className="font-mono text-xs">{new Date(value).toLocaleString()}</span>;
}
