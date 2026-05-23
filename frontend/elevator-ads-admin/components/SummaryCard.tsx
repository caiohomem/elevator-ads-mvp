export function SummaryCard({
  label,
  value,
  description,
}: {
  label: string;
  value: string | number;
  description?: string;
}) {
  return (
    <div className="panel rounded-[28px] p-5">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">{label}</p>
      <p className="mt-4 text-3xl font-semibold tracking-[-0.06em] text-[var(--foreground)]">{value}</p>
      {description ? <p className="mt-3 text-sm leading-6 text-[var(--muted)]">{description}</p> : null}
    </div>
  );
}
