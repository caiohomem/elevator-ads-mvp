"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import { useTranslation } from "@/lib/i18n";
import { navigationItems } from "@/lib/navigation";

export function MobileNav() {
  const pathname = usePathname();
  const { dictionary } = useTranslation();
  const [open, setOpen] = useState(false);

  return (
    <div className="md:hidden">
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="panel-strong inline-flex items-center rounded-2xl px-3 py-2 text-sm font-medium text-[var(--foreground)]"
      >
        {dictionary.common.mobileMenu}
      </button>

      {open ? (
        <div className="fixed inset-0 z-50 flex">
          <button
            type="button"
            aria-label={dictionary.common.close}
            className="flex-1 bg-slate-950/45 backdrop-blur-sm"
            onClick={() => setOpen(false)}
          />
          <div className="flex h-full w-[82vw] max-w-[320px] flex-col bg-[var(--sidebar)] px-5 py-6 text-[var(--sidebar-foreground)] shadow-2xl">
            <div className="mb-6 flex items-center justify-between">
              <div>
                <p className="text-[0.7rem] font-semibold uppercase tracking-[0.28em] text-[var(--accent)]">
                  Elevator Ads
                </p>
                <p className="mt-2 text-lg font-semibold">{dictionary.app.name}</p>
              </div>
              <button
                type="button"
                onClick={() => setOpen(false)}
                className="rounded-xl border border-white/10 px-3 py-2 text-sm"
              >
                {dictionary.common.close}
              </button>
            </div>
            <nav className="space-y-1.5">
              {navigationItems.map((item) => {
                const active = pathname === item.href;
                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    onClick={() => setOpen(false)}
                    className={`block rounded-2xl px-4 py-3 text-sm font-medium transition ${
                      active
                        ? "bg-white/10 text-white"
                        : "text-[var(--sidebar-muted)] hover:bg-white/5 hover:text-white"
                    }`}
                  >
                    {dictionary.nav[item.key]}
                  </Link>
                );
              })}
            </nav>
          </div>
        </div>
      ) : null}
    </div>
  );
}
