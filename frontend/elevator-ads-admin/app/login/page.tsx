"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { LanguageSelector } from "@/components/LanguageSelector";
import { ThemeToggle } from "@/components/ThemeToggle";
import { useAuth } from "@/lib/auth/context";
import { useTranslation } from "@/lib/i18n";

interface LoginResponse {
  token: string;
  role: string;
  expiresAt: string;
}

export default function LoginPage() {
  const router = useRouter();
  const { dictionary } = useTranslation();
  const { isAuthenticated, isHydrated, login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (isHydrated && isAuthenticated) {
      router.replace(getNextPath());
    }
  }, [isAuthenticated, isHydrated, router]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ username, password }),
      });

      if (response.status === 401) {
        setError(dictionary.login.invalidCredentials);
        return;
      }

      if (!response.ok) {
        setError(dictionary.login.unexpectedError);
        return;
      }

      const body = (await response.json()) as LoginResponse;
      login(body.token, body.role);
      router.replace(getNextPath());
    } catch {
      setError(dictionary.login.unexpectedError);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden px-4 py-10 sm:px-6">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(199,103,47,0.16),transparent_34%)]" />
      <div className="relative w-full max-w-md space-y-6">
        <div className="flex items-center justify-end gap-2 sm:gap-3">
          <LanguageSelector />
          <ThemeToggle />
        </div>

        <section className="panel rounded-[32px] p-6 sm:p-8">
          <p className="text-[0.72rem] font-semibold uppercase tracking-[0.28em] text-[var(--accent)]">
            Elevator Ads
          </p>
          <h1 className="mt-4 text-3xl font-semibold tracking-[-0.05em] text-[var(--foreground)]">
            {dictionary.login.title}
          </h1>
          <p className="mt-3 text-sm leading-6 text-[var(--muted)]">{dictionary.login.subtitle}</p>

          <form className="mt-8 space-y-5" onSubmit={handleSubmit}>
            <label className="block">
              <span className="mb-2 block text-sm font-medium text-[var(--foreground)]">
                {dictionary.login.username}
              </span>
              <input
                type="text"
                autoComplete="username"
                value={username}
                onChange={(event) => setUsername(event.target.value)}
                className="w-full rounded-2xl border border-[var(--panel-border)] bg-[var(--panel-strong)] px-4 py-3 text-[var(--foreground)] outline-none transition focus:border-[var(--accent)]"
                required
              />
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-medium text-[var(--foreground)]">
                {dictionary.login.password}
              </span>
              <input
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                className="w-full rounded-2xl border border-[var(--panel-border)] bg-[var(--panel-strong)] px-4 py-3 text-[var(--foreground)] outline-none transition focus:border-[var(--accent)]"
                required
              />
            </label>

            {error ? (
              <div
                className="rounded-2xl border border-[var(--panel-border)] bg-[var(--accent-soft)] px-4 py-3 text-sm text-[var(--foreground)]"
                role="alert"
              >
                {error}
              </div>
            ) : null}

            <button
              type="submit"
              disabled={submitting}
              className="w-full rounded-2xl bg-[var(--accent)] px-4 py-3 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-70"
            >
              {submitting ? dictionary.login.submitting : dictionary.login.submit}
            </button>
          </form>
        </section>
      </div>
    </div>
  );
}

function getNextPath() {
  if (typeof window === "undefined") {
    return "/";
  }

  return new URLSearchParams(window.location.search).get("next") || "/";
}
