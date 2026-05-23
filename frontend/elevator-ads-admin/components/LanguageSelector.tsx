"use client";

import type { Locale } from "@/lib/i18n";
import { useTranslation } from "@/lib/i18n";

export function LanguageSelector() {
  const { locale, setLocale, dictionary } = useTranslation();

  return (
    <label className="panel-strong inline-flex items-center gap-2 rounded-2xl px-3 py-2 text-sm font-medium text-[var(--foreground)]">
      <span className="hidden text-[var(--muted)] sm:inline">{dictionary.common.language}</span>
      <select
        aria-label={dictionary.common.language}
        value={locale}
        onChange={(event) => setLocale(event.target.value as Locale)}
        className="bg-transparent outline-none"
      >
        <option value="en" className="text-slate-900">
          EN
        </option>
        <option value="pt" className="text-slate-900">
          PT
        </option>
      </select>
    </label>
  );
}
