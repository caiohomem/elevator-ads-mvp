"use client";

import { useTranslation } from "@/lib/i18n";

export function ErrorState({
  message,
  onRetry,
  compact = false,
}: {
  message?: string;
  onRetry: () => void;
  compact?: boolean;
}) {
  const { dictionary } = useTranslation();

  return (
    <div
      className={`rounded-[28px] border border-[var(--panel-border)] bg-[var(--accent-soft)] px-5 text-sm text-[var(--foreground)] ${
        compact ? "py-4" : "py-8 text-center"
      }`}
      role="alert"
    >
      <div className={`flex gap-4 ${compact ? "items-center justify-between" : "flex-col items-center"}`}>
        <div>
          <p className="font-semibold">{dictionary.common.errorLoading}</p>
          <p className="mt-1 text-[var(--muted)]">{message ?? dictionary.common.apiUnavailable}</p>
        </div>
        <button
          type="button"
          onClick={onRetry}
          className="shrink-0 rounded-2xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
        >
          {dictionary.common.retry}
        </button>
      </div>
    </div>
  );
}
