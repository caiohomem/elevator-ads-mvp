"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { useTranslation } from "@/lib/i18n";
import { useTheme } from "@/lib/theme";

function SettingCard({
  title,
  value,
  description,
}: {
  title: string;
  value: string;
  description: string;
}) {
  return (
    <div className="panel rounded-[28px] p-5">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">{title}</p>
      <p className="mt-4 text-2xl font-semibold tracking-[-0.05em] text-[var(--foreground)]">{value}</p>
      <p className="mt-3 text-sm leading-6 text-[var(--muted)]">{description}</p>
    </div>
  );
}

export default function SettingsPage() {
  const { dictionary, locale } = useTranslation();
  const { theme } = useTheme();

  return (
    <ClientPageFrame section="settings">
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <SettingCard
          title={dictionary.settings.themePreference}
          value={theme === "dark" ? dictionary.common.dark : dictionary.common.light}
          description={`${dictionary.common.currentTheme}: ${theme}`}
        />
        <SettingCard
          title={dictionary.settings.languagePreference}
          value={locale.toUpperCase()}
          description={`${dictionary.common.currentLanguage}: ${locale}`}
        />
        <SettingCard
          title={dictionary.settings.environmentInfo}
          value={process.env.NODE_ENV}
          description={dictionary.settings.environmentDescription}
        />
      </div>
    </ClientPageFrame>
  );
}
