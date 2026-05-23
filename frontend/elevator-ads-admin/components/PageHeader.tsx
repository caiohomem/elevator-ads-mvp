export function PageHeader({
  title,
  description,
  action,
}: {
  title: string;
  description: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="mb-6 flex flex-col gap-4 border-b border-[var(--panel-border)] pb-5 sm:flex-row sm:items-end sm:justify-between">
      <div className="space-y-2">
        <p className="text-[0.7rem] font-semibold uppercase tracking-[0.28em] text-[var(--accent)]">
          Operations
        </p>
        <h1 className="text-3xl font-semibold tracking-[-0.05em] text-[var(--foreground)] sm:text-4xl">
          {title}
        </h1>
        <p className="max-w-3xl text-sm leading-6 text-[var(--muted)] sm:text-base">{description}</p>
      </div>
      {action ? <div className="shrink-0">{action}</div> : null}
    </div>
  );
}
