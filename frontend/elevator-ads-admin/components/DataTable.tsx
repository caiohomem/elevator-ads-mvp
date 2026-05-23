import { EmptyState } from "@/components/EmptyState";

export interface TableColumn<T> {
  key: string;
  header: string;
  render: (row: T) => React.ReactNode;
  className?: string;
}

export function DataTable<T>({
  columns,
  rows,
  getRowKey,
}: {
  columns: TableColumn<T>[];
  rows: T[];
  getRowKey: (row: T) => string;
}) {
  if (!rows.length) {
    return <EmptyState />;
  }

  return (
    <div className="panel overflow-hidden rounded-[28px]">
      <div className="overflow-x-auto">
        <table className="min-w-full border-separate border-spacing-0">
          <thead>
            <tr>
              {columns.map((column) => (
                <th
                  key={column.key}
                  className="border-b border-[var(--panel-border)] px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-[var(--muted)] first:pl-6 last:pr-6"
                >
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={getRowKey(row)} className="odd:bg-white/20 dark:odd:bg-white/[0.02]">
                {columns.map((column) => (
                  <td
                    key={column.key}
                    className={`border-b border-[var(--panel-border)] px-4 py-4 align-top text-sm text-[var(--foreground)] first:pl-6 last:pr-6 ${column.className ?? ""}`}
                  >
                    {column.render(row)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
