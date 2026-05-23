"use client";

import type { TranslationDictionary } from "@/lib/i18n/en";
import { PageHeader } from "@/components/PageHeader";
import { useTranslation } from "@/lib/i18n";

type PageSection = keyof TranslationDictionary["pages"];

export function ClientPageFrame({
  section,
  action,
  children,
}: {
  section: PageSection;
  action?: React.ReactNode;
  children: React.ReactNode;
}) {
  const { dictionary } = useTranslation();
  const page = dictionary.pages[section];

  return (
    <section className="space-y-6">
      <PageHeader title={page.title} description={page.description} action={action} />
      {children}
    </section>
  );
}
