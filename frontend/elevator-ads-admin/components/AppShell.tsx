"use client";

import { usePathname } from "next/navigation";
import { LanguageSelector } from "@/components/LanguageSelector";
import { MobileNav } from "@/components/MobileNav";
import { ShellTitle } from "@/components/ShellTitle";
import { Sidebar } from "@/components/Sidebar";
import { ThemeToggle } from "@/components/ThemeToggle";
import { useAuth } from "@/lib/auth/context";
import { useTranslation } from "@/lib/i18n";

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const { dictionary } = useTranslation();
  const { isAuthenticated, role, logout } = useAuth();

  if (pathname === "/login") {
    return <>{children}</>;
  }

  return (
    <div className="min-h-screen text-[var(--foreground)]">
      <div className="mx-auto flex min-h-screen max-w-[1600px]">
        <Sidebar />
        <div className="flex min-h-screen min-w-0 flex-1 flex-col">
          <header className="sticky top-0 z-30 border-b border-[var(--panel-border)] bg-[color:var(--background)]/80 backdrop-blur-xl">
            <div className="flex items-center justify-between gap-3 px-4 py-4 sm:px-6 lg:px-8">
              <div className="flex items-center gap-3">
                <MobileNav />
                <ShellTitle />
              </div>
              <div className="flex items-center gap-2 sm:gap-3">
                <LanguageSelector />
                <ThemeToggle />
                {isAuthenticated && role ? (
                  <>
                    <div className="hidden rounded-2xl border border-[var(--panel-border)] bg-[var(--panel-strong)] px-3 py-2 text-sm font-medium text-[var(--foreground)] lg:block">
                      {dictionary.login.loggedInAs}: {role}
                    </div>
                    <button
                      type="button"
                      onClick={logout}
                      className="rounded-2xl border border-[var(--panel-border)] bg-[var(--panel-strong)] px-3 py-2 text-sm font-medium text-[var(--foreground)] transition hover:translate-y-[-1px]"
                    >
                      {dictionary.login.logout}
                    </button>
                  </>
                ) : null}
              </div>
            </div>
          </header>
          <main className="flex-1 px-4 py-6 sm:px-6 lg:px-8 lg:py-8">{children}</main>
        </div>
      </div>
    </div>
  );
}
