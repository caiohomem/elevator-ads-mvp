"use client";

import Link from "next/link";
import { useTranslation } from "@/lib/i18n";

export default function ForbiddenPage() {
  const { dictionary } = useTranslation();

  return (
    <section className="mx-auto flex min-h-[calc(100vh-9rem)] max-w-3xl items-center justify-center">
      <div className="panel w-full rounded-[32px] p-7 text-center sm:p-10">
        <p className="text-[0.72rem] font-semibold uppercase tracking-[0.28em] text-[var(--accent)]">
          Access
        </p>
        <h1 className="mt-4 text-3xl font-semibold tracking-[-0.05em] text-[var(--foreground)] sm:text-4xl">
          {dictionary.login.forbiddenTitle}
        </h1>
        <p className="mx-auto mt-4 max-w-2xl text-sm leading-7 text-[var(--muted)] sm:text-base">
          {dictionary.login.forbiddenMessage}
        </p>
        <Link
          href="/"
          className="mt-8 inline-flex rounded-2xl bg-[var(--accent)] px-5 py-3 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
        >
          {dictionary.login.forbiddenAction}
        </Link>
      </div>
    </section>
  );
}
