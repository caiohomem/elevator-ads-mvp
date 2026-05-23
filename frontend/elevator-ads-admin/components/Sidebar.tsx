"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslation } from "@/lib/i18n";
import { navigationItems } from "@/lib/navigation";

export function Sidebar() {
  const pathname = usePathname();
  const { dictionary } = useTranslation();

  return (
    <aside className="sticky top-0 hidden h-screen w-[280px] shrink-0 border-r border-white/6 bg-[var(--sidebar)] px-5 py-6 text-[var(--sidebar-foreground)] md:flex md:flex-col">
      <div className="mb-8 border-b border-white/8 pb-6">
        <p className="text-[0.7rem] font-semibold uppercase tracking-[0.32em] text-[var(--accent)]">
          Elevator Ads
        </p>
        <h1 className="mt-3 text-2xl font-semibold tracking-[-0.04em]">{dictionary.app.name}</h1>
        <p className="mt-2 max-w-[18rem] text-sm leading-6 text-[var(--sidebar-muted)]">
          {dictionary.common.updatedForMvp}
        </p>
      </div>
      <nav className="space-y-1.5">
        {navigationItems.map((item) => {
          const active = pathname === item.href;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={`group flex items-center justify-between rounded-2xl px-4 py-3 text-sm font-medium transition ${
                active
                  ? "bg-white/10 text-white shadow-[inset_0_0_0_1px_rgba(255,255,255,0.06)]"
                  : "text-[var(--sidebar-muted)] hover:bg-white/5 hover:text-white"
              }`}
            >
              <span>{dictionary.nav[item.key]}</span>
              <span
                className={`h-2 w-2 rounded-full transition ${
                  active ? "bg-[var(--accent)]" : "bg-white/12 group-hover:bg-white/25"
                }`}
              />
            </Link>
          );
        })}
      </nav>
      <div className="mt-auto rounded-3xl border border-white/8 bg-white/4 p-4">
        <p className="text-xs uppercase tracking-[0.24em] text-[var(--sidebar-muted)]">
          {dictionary.common.mockedData}
        </p>
        <p className="mt-2 text-sm leading-6 text-[var(--sidebar-foreground)]">
          Daily playlist delivery only. Real-time serving is intentionally out of scope.
        </p>
      </div>
    </aside>
  );
}
