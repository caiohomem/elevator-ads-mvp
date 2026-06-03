"use client";

import { useTranslation } from "@/lib/i18n";

const PAGE_SIZE_OPTIONS = [10, 20, 50] as const;

export function PaginationControls({
  page,
  totalPages,
  totalItems,
  pageSize,
  onPageChange,
  onPageSizeChange,
}: {
  page: number;
  totalPages: number;
  totalItems: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}) {
  const { dictionary } = useTranslation();
  const startItem = totalItems === 0 ? 0 : (page - 1) * pageSize + 1;
  const endItem = totalItems === 0 ? 0 : Math.min(page * pageSize, totalItems);
  const isAtFirstPage = page <= 1;
  const isAtLastPage = totalPages > 0 ? page >= totalPages : true;

  return (
    <div className="panel flex flex-col gap-3 rounded-[28px] px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="text-sm text-[var(--muted)]">
        {dictionary.pagination.showing} <span className="font-semibold text-[var(--foreground)]">{startItem}-{endItem}</span>{" "}
        {dictionary.pagination.of} <span className="font-semibold text-[var(--foreground)]">{totalItems}</span>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <label className="flex items-center gap-2 text-sm text-[var(--muted)]">
          <span>{dictionary.pagination.pageSize}</span>
          <select
            value={pageSize}
            onChange={(event) => onPageSizeChange(Number(event.target.value))}
            className="rounded-2xl border border-[var(--panel-border)] bg-white/60 px-3 py-2 text-sm text-[var(--foreground)] outline-none transition focus:ring-2 focus:ring-[var(--accent)]/40 dark:bg-white/5"
          >
            {PAGE_SIZE_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>

        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => onPageChange(Math.max(1, page - 1))}
            disabled={isAtFirstPage}
            className="rounded-2xl border border-[var(--panel-border)] px-4 py-2.5 text-sm font-semibold text-[var(--foreground)] transition hover:bg-white/30 disabled:cursor-not-allowed disabled:opacity-60 dark:hover:bg-white/5"
          >
            {dictionary.pagination.prev}
          </button>
          <button
            type="button"
            onClick={() => onPageChange(page + 1)}
            disabled={isAtLastPage}
            className="rounded-2xl border border-[var(--panel-border)] px-4 py-2.5 text-sm font-semibold text-[var(--foreground)] transition hover:bg-white/30 disabled:cursor-not-allowed disabled:opacity-60 dark:hover:bg-white/5"
          >
            {dictionary.pagination.next}
          </button>
        </div>
      </div>
    </div>
  );
}
