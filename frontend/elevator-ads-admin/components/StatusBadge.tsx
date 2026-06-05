const statusClasses: Record<string, string> = {
  Active: "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300",
  Inactive: "bg-slate-500/12 text-slate-700 dark:text-slate-300",
  Offline: "bg-rose-500/12 text-rose-700 dark:text-rose-300",
  Maintenance: "bg-amber-500/14 text-amber-800 dark:text-amber-300",
  Draft: "bg-slate-500/12 text-slate-700 dark:text-slate-300",
  PendingReview: "bg-amber-500/14 text-amber-800 dark:text-amber-300",
  Approved: "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300",
  Rejected: "bg-rose-500/12 text-rose-700 dark:text-rose-300",
  Scheduled: "bg-sky-500/12 text-sky-700 dark:text-sky-300",
  Paused: "bg-fuchsia-500/12 text-fuchsia-700 dark:text-fuchsia-300",
  Published: "bg-sky-500/12 text-sky-700 dark:text-sky-300",
  Downloaded: "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300",
  Expired: "bg-slate-500/12 text-slate-700 dark:text-slate-300",
};

export function StatusBadge({ status, label }: { status: string; label?: string }) {
  return (
    <span
      className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold tracking-[0.02em] ${
        statusClasses[status] ?? "bg-slate-500/12 text-slate-700 dark:text-slate-300"
      }`}
    >
      {label ?? status}
    </span>
  );
}
