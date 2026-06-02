"use client";

import { useTranslation } from "@/lib/i18n";

export function LoadingState() {
  const { dictionary } = useTranslation();

  return (
    <div className="panel rounded-[28px] p-6" role="status" aria-live="polite">
      <div className="mb-4 text-sm font-semibold text-[var(--foreground)]">{dictionary.common.loading}</div>
      <div className="space-y-3">
        {[0, 1, 2].map((item) => (
          <div
            key={item}
            className="h-12 animate-pulse rounded-2xl border border-[var(--panel-border)] bg-[var(--accent-soft)]"
          />
        ))}
      </div>
    </div>
  );
}
