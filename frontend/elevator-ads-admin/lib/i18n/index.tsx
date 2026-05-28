"use client";

import { createContext, useContext, useEffect, useState } from "react";
import { en, type TranslationDictionary } from "@/lib/i18n/en";
import { pt } from "@/lib/i18n/pt";

const LOCALE_STORAGE_KEY = "elevator-ads-admin-locale";

const dictionaries = {
  en,
  pt,
} as const;

export type Locale = keyof typeof dictionaries;

interface I18nContextValue {
  locale: Locale;
  dictionary: TranslationDictionary;
  setLocale: (locale: Locale) => void;
}

const I18nContext = createContext<I18nContextValue | null>(null);

export function I18nProvider({ children }: { children: React.ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>("en");
  const [isHydrated, setIsHydrated] = useState(false);

  useEffect(() => {
    const stored = window.localStorage.getItem(LOCALE_STORAGE_KEY);
    const initialLocale = stored === "en" || stored === "pt" ? stored : "en";

    setLocaleState(initialLocale);
    document.documentElement.lang = initialLocale;
    setIsHydrated(true);
  }, []);

  useEffect(() => {
    if (!isHydrated) {
      return;
    }

    document.documentElement.lang = locale;
    window.localStorage.setItem(LOCALE_STORAGE_KEY, locale);
  }, [isHydrated, locale]);

  const setLocale = (nextLocale: Locale) => {
    setLocaleState(nextLocale);
  };

  const value = {
    locale,
    dictionary: dictionaries[locale],
    setLocale,
  };

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useTranslation() {
  const context = useContext(I18nContext);

  if (!context) {
    throw new Error("useTranslation must be used within I18nProvider");
  }

  return context;
}
