"use client";

import { useState } from "react";
import { Modal } from "@/components/Modal";
import { StatusBadge } from "@/components/StatusBadge";
import { publishPlaylist } from "@/lib/api/playlists";
import { useTranslation } from "@/lib/i18n";
import type { DailyPlaylist } from "@/lib/types";

export function PlaylistDetailPanel({
  playlist,
  onClose,
  onPublished,
}: {
  playlist: DailyPlaylist;
  onClose: () => void;
  onPublished?: (published: DailyPlaylist) => void;
}) {
  const { dictionary } = useTranslation();
  const labels = dictionary.pages.playlists;

  const [publishing, setPublishing] = useState(false);
  const [publishError, setPublishError] = useState<string | null>(null);
  const [status, setStatus] = useState<DailyPlaylist["status"]>(playlist.status);
  const [publishedAt, setPublishedAt] = useState<string | null>(playlist.publishedAt);

  const canPublish = status === "Draft";
  const isDownloadable = status === "Published" || status === "Downloaded";
  const statusDescription = labels.status[status.toLowerCase() as keyof typeof labels.status];

  const handlePublish = async () => {
    setPublishError(null);
    setPublishing(true);

    const result = await publishPlaylist(playlist.id);

    setPublishing(false);

    if (!result.ok) {
      setPublishError(result.message || labels.publishError);
      return;
    }

    const formattedPublishedAt = result.data.publishedAt
      ? result.data.publishedAt.replace("T", " ").slice(0, 16)
      : new Date().toISOString().replace("T", " ").slice(0, 16);

    setStatus("Published");
    setPublishedAt(formattedPublishedAt);
    onPublished?.({
      ...playlist,
      status: "Published",
      publishedAt: formattedPublishedAt,
    });
  };

  return (
    <Modal open onClose={onClose} title={`${dictionary.nav.playlists} · ${playlist.date}`}>
      <div className="space-y-5">
        {publishError ? (
          <div
            className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-700 dark:text-rose-300"
            role="alert"
          >
            {publishError}
          </div>
        ) : null}

        <div className="grid grid-cols-1 gap-3 rounded-2xl border border-[var(--panel-border)] bg-white/40 px-4 py-4 text-sm dark:bg-white/[0.02] sm:grid-cols-2">
          <Field label={labels.detailScreen} value={playlist.screenName} />
          <Field label={labels.detailBuilding} value={playlist.buildingName} />
          <Field label={labels.detailDate} value={playlist.date} />
          <Field label={labels.detailVersion} value={<span className="font-mono text-xs">{playlist.version}</span>} />
          <Field
            label={labels.detailStatus}
            value={
              <div className="flex flex-col items-start gap-1">
                <StatusBadge status={status} />
                {statusDescription ? <p className="text-xs text-[var(--muted)]">{statusDescription}</p> : null}
              </div>
            }
          />
          <Field
            label={labels.detailGeneratedAt}
            value={<span className="font-mono text-xs">{playlist.generatedAt}</span>}
          />
          {publishedAt ? (
            <Field label={labels.detailPublishedAt} value={<span className="font-mono text-xs">{publishedAt}</span>} />
          ) : null}
        </div>

        {isDownloadable ? (
          <div
            className="rounded-2xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-700 dark:text-emerald-300"
            role="status"
          >
            {labels.publishedNote}
          </div>
        ) : null}

        <div>
          <h3 className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">
            {labels.detailItems}
          </h3>
          {playlist.items.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-[var(--panel-border)] px-4 py-6 text-center text-sm text-[var(--muted)]">
              {dictionary.common.noData}
            </div>
          ) : (
            <div className="overflow-hidden rounded-2xl border border-[var(--panel-border)]">
              <table className="min-w-full border-separate border-spacing-0">
                <thead>
                  <tr>
                    <th className="border-b border-[var(--panel-border)] bg-white/40 px-3 py-2 text-left text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)] dark:bg-white/[0.02]">
                      {labels.itemOrder}
                    </th>
                    <th className="border-b border-[var(--panel-border)] bg-white/40 px-3 py-2 text-left text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)] dark:bg-white/[0.02]">
                      {labels.itemCampaign}
                    </th>
                    <th className="border-b border-[var(--panel-border)] bg-white/40 px-3 py-2 text-left text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)] dark:bg-white/[0.02]">
                      {labels.itemCreative}
                    </th>
                    <th className="border-b border-[var(--panel-border)] bg-white/40 px-3 py-2 text-left text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)] dark:bg-white/[0.02]">
                      {labels.itemMediaType}
                    </th>
                    <th className="border-b border-[var(--panel-border)] bg-white/40 px-3 py-2 text-right text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted)] dark:bg-white/[0.02]">
                      {labels.itemDuration}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {playlist.items.map((item) => (
                    <tr key={item.id}>
                      <td className="border-b border-[var(--panel-border)] px-3 py-2 text-sm text-[var(--foreground)]">
                        <span className="font-mono text-xs">{item.position}</span>
                      </td>
                      <td className="border-b border-[var(--panel-border)] px-3 py-2 text-sm text-[var(--foreground)]">
                        {item.campaignName}
                      </td>
                      <td className="border-b border-[var(--panel-border)] px-3 py-2 text-sm text-[var(--foreground)]">
                        {item.creativeName}
                      </td>
                      <td className="border-b border-[var(--panel-border)] px-3 py-2 text-sm text-[var(--foreground)]">
                        {item.mediaType}
                      </td>
                      <td className="border-b border-[var(--panel-border)] px-3 py-2 text-right text-sm text-[var(--foreground)]">
                        <span className="font-mono text-xs">{item.durationSeconds}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="flex flex-col-reverse gap-2 pt-2 sm:flex-row sm:items-center sm:justify-end">
          <button
            type="button"
            onClick={onClose}
            className="rounded-2xl border border-[var(--panel-border)] px-4 py-2.5 text-sm font-semibold text-[var(--foreground)] transition hover:bg-white/30 dark:hover:bg-white/5"
          >
            {dictionary.forms.cancel}
          </button>
          {canPublish ? (
            <button
              type="button"
              onClick={handlePublish}
              disabled={publishing}
              className="rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-60"
            >
              {publishing ? labels.publishing : labels.publish}
            </button>
          ) : null}
        </div>
      </div>
    </Modal>
  );
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="space-y-1">
      <p className="text-[0.7rem] font-semibold uppercase tracking-[0.18em] text-[var(--muted)]">{label}</p>
      <div className="text-sm text-[var(--foreground)]">{value}</div>
    </div>
  );
}
