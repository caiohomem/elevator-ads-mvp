"use client";

import { useEffect, useState } from "react";
import { useTranslation } from "@/lib/i18n";

export type TableStatusOption = {
  value: string;
  label: string;
};

export function TableFilters({
  search,
  onSearchChange,
  status,
  statusOptions,
  onStatusChange,
  placeholder,
}: {
  search: string;
  onSearchChange: (search: string) => void;
  status?: string;
  statusOptions?: TableStatusOption[];
  onStatusChange?: (status?: string) => void;
  placeholder?: string;
}) {
  const { dictionary } = useTranslation();
  const [searchInput, setSearchInput] = useState(search);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      if (searchInput !== search) {
        onSearchChange(searchInput);
      }
    }, 300);

    return () => window.clearTimeout(timeout);
  }, [onSearchChange, search, searchInput]);

  return (
    <div className="panel flex flex-col gap-3 rounded-[28px] px-5 py-4 lg:flex-row lg:items-end lg:justify-between">
      <label className="flex flex-1 flex-col gap-1.5">
        <span className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">
          {dictionary.filters.search}
        </span>
        <input
          type="search"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          placeholder={placeholder ?? dictionary.filters.search}
          className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
        />
      </label>

      {statusOptions && onStatusChange ? (
        <label className="flex flex-col gap-1.5 sm:min-w-56">
          <span className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">
            {dictionary.filters.status}
          </span>
          <select
            value={status ?? ""}
            onChange={(event) => onStatusChange(event.target.value || undefined)}
            className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3.5 py-2.5 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
          >
            <option value="">{dictionary.filters.all}</option>
            {statusOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      ) : null}
    </div>
  );
}
