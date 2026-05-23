"use client";

import { useTranslation } from "@/lib/i18n";

export function EmptyState() {
  const { dictionary } = useTranslation();

  return (
    <div className="rounded-[28px] border border-dashed border-[var(--panel-border)] px-6 py-12 text-center text-sm text-[var(--muted)]">
      {dictionary.common.noData}
    </div>
  );
}
