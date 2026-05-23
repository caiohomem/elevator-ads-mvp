"use client";

import { useTranslation } from "@/lib/i18n";

export function ShellTitle() {
  const { dictionary } = useTranslation();

  return (
    <div className="min-w-0">
      <p className="text-[0.7rem] font-semibold uppercase tracking-[0.28em] text-[var(--accent)]">
        DOOH Admin
      </p>
      <div className="truncate text-sm font-semibold text-[var(--foreground)] sm:text-base">
        {dictionary.app.name}
      </div>
      <div className="hidden text-xs text-[var(--muted)] sm:block">{dictionary.app.subtitle}</div>
    </div>
  );
}
