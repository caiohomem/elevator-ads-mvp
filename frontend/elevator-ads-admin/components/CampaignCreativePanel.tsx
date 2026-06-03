"use client";

import { useCallback, useEffect, useState } from "react";
import { LoadingState } from "@/components/LoadingState";
import { Modal } from "@/components/Modal";
import {
  assignCreative,
  getCampaignCreatives,
  removeCreative,
  type ApiCampaignCreative,
} from "@/lib/api/campaigns";
import { getCreativesList } from "@/lib/api/creatives";
import { useTranslation } from "@/lib/i18n";
import type { ApiCreative } from "@/lib/types";

type LoadState = { status: "loading" } | { status: "error"; message: string } | { status: "ok" };

type Action = "assign" | "remove";
type ActionInFlight = { creativeId: string; action: Action };

export function CampaignCreativePanel({
  campaignId,
  advertiserId,
  onClose,
}: {
  campaignId: string;
  advertiserId: string;
  onClose: () => void;
}) {
  const { dictionary } = useTranslation();
  const forms = dictionary.forms;

  const [loadState, setLoadState] = useState<LoadState>({ status: "loading" });
  const [loadError, setLoadError] = useState<string | null>(null);
  const [assignments, setAssignments] = useState<ApiCampaignCreative[]>([]);
  const [creatives, setCreatives] = useState<ApiCreative[]>([]);
  const [reloadKey, setReloadKey] = useState(0);
  const [actionInFlight, setActionInFlight] = useState<ActionInFlight | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    Promise.all([getCampaignCreatives(campaignId), getCreativesList()])
      .then(([assignmentsResult, creativesResult]) => {
        if (!active) {
          return;
        }

        if (!assignmentsResult.ok) {
          setLoadError(assignmentsResult.message);
          setLoadState({ status: "error", message: assignmentsResult.message });
          return;
        }

        if (!creativesResult.ok) {
          setLoadError(creativesResult.message);
          setLoadState({ status: "error", message: creativesResult.message });
          return;
        }

        setAssignments(assignmentsResult.data);
        setCreatives(creativesResult.data);
        setLoadState({ status: "ok" });
      })
      .catch((error) => {
        if (!active) {
          return;
        }

        const message = error instanceof Error ? error.message : "Unable to load creatives.";
        setLoadError(message);
        setLoadState({ status: "error", message });
      });

    return () => {
      active = false;
    };
  }, [campaignId, reloadKey]);

  const refresh = useCallback(() => {
    setReloadKey((key) => key + 1);
  }, []);

  const creativesById = new Map(creatives.map((creative) => [creative.id, creative] as const));
  const assignedIds = new Set(assignments.map((assignment) => assignment.creativeId));

  const assignedCreatives = assignments
    .map((assignment) => creativesById.get(assignment.creativeId))
    .filter((creative): creative is ApiCreative => Boolean(creative));

  const availableCreatives = creatives.filter(
    (creative) => creative.advertiserId === advertiserId && creative.approvalStatus === "Approved" && !assignedIds.has(creative.id),
  );

  const runAction = async (creative: ApiCreative, action: Action) => {
    setActionError(null);
    setActionInFlight({ creativeId: creative.id, action });

    const result = action === "assign"
      ? await assignCreative(campaignId, creative.id)
      : await removeCreative(campaignId, creative.id);

    setActionInFlight(null);

    if (!result.ok) {
      setActionError(result.message || forms.actionFailed);
      return;
    }

    await refresh();
  };

  return (
    <Modal open onClose={onClose} title={forms.manageCreatives}>
      {actionError ? (
        <div
          className="mb-4 rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-700 dark:text-rose-300"
          role="alert"
        >
          {actionError}
        </div>
      ) : null}

      {loadState.status === "loading" ? <LoadingState /> : null}
      {loadState.status === "error" ? (
        <div
          className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-700 dark:text-rose-300"
          role="alert"
        >
          {loadError ?? dictionary.common.apiUnavailable}
        </div>
      ) : null}

      {loadState.status === "ok" ? (
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <CreativeList
            title={forms.assignedCreatives}
            emptyLabel={dictionary.common.noData}
            creatives={assignedCreatives}
            renderAction={(creative) => {
              const isActing = actionInFlight?.creativeId === creative.id;
              return (
                <button
                  type="button"
                  onClick={() => runAction(creative, "remove")}
                  disabled={Boolean(actionInFlight)}
                  className="rounded-full border border-rose-500/40 bg-rose-500/10 px-3 py-1 text-xs font-semibold text-rose-700 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-60 dark:text-rose-300"
                >
                  {isActing && actionInFlight?.action === "remove" ? "..." : forms.remove}
                </button>
              );
            }}
          />

          <CreativeList
            title={forms.availableCreatives}
            emptyLabel={dictionary.common.noData}
            creatives={availableCreatives}
            renderAction={(creative) => {
              const isActing = actionInFlight?.creativeId === creative.id;
              return (
                <button
                  type="button"
                  onClick={() => runAction(creative, "assign")}
                  disabled={Boolean(actionInFlight)}
                  className="rounded-full border border-emerald-500/40 bg-emerald-500/10 px-3 py-1 text-xs font-semibold text-emerald-700 transition hover:bg-emerald-500/20 disabled:cursor-not-allowed disabled:opacity-60 dark:text-emerald-300"
                >
                  {isActing && actionInFlight?.action === "assign" ? "..." : forms.assign}
                </button>
              );
            }}
          />
        </div>
      ) : null}

      <div className="mt-6 flex justify-end">
        <button
          type="button"
          onClick={onClose}
          className="rounded-2xl border border-[var(--panel-border)] px-4 py-2.5 text-sm font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
        >
          {forms.cancel}
        </button>
      </div>
    </Modal>
  );
}

function CreativeList({
  title,
  emptyLabel,
  creatives,
  renderAction,
}: {
  title: string;
  emptyLabel: string;
  creatives: ApiCreative[];
  renderAction: (creative: ApiCreative) => React.ReactNode;
}) {
  return (
    <div>
      <h3 className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">{title}</h3>
      {creatives.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-[var(--panel-border)] px-4 py-6 text-center text-sm text-[var(--muted)]">
          {emptyLabel}
        </div>
      ) : (
        <ul className="space-y-2">
          {creatives.map((creative) => (
            <li
              key={creative.id}
              className="flex items-center justify-between gap-3 rounded-2xl border border-[var(--panel-border)] bg-white/40 px-4 py-3 text-sm dark:bg-white/[0.02]"
            >
              <div className="min-w-0 flex-1">
                <p className="truncate font-semibold text-[var(--foreground)]">{creative.name}</p>
                <p className="text-xs text-[var(--muted)]">
                  {creative.mediaType} · {creative.durationSeconds}s
                </p>
              </div>
              {renderAction(creative)}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
