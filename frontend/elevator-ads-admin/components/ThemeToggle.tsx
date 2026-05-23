"use client";

import { useTranslation } from "@/lib/i18n";
import { useTheme } from "@/lib/theme";

export function ThemeToggle() {
  const { theme, toggleTheme } = useTheme();
  const { dictionary } = useTranslation();

  return (
    <button
      type="button"
      onClick={toggleTheme}
      className="panel-strong inline-flex min-w-[96px] items-center justify-center rounded-2xl px-3 py-2 text-sm font-medium text-[var(--foreground)] transition hover:translate-y-[-1px]"
    >
      {dictionary.common.theme}: {theme === "dark" ? dictionary.common.dark : dictionary.common.light}
    </button>
  );
}
