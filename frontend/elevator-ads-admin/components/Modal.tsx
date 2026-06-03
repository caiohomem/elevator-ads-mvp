"use client";

import { useEffect } from "react";
import { createPortal } from "react-dom";
import { useTranslation } from "@/lib/i18n";

export function Modal({
  open,
  onClose,
  title,
  children,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}) {
  const { dictionary } = useTranslation();

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      document.body.style.overflow = previousOverflow;
    };
  }, [open, onClose]);

  if (!open || typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/55 px-3 py-4 backdrop-blur-sm sm:items-center sm:px-6"
      onClick={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
      role="dialog"
      aria-modal="true"
      aria-label={title}
    >
      <div className="panel w-full max-w-xl overflow-hidden rounded-[28px]">
        <div className="flex items-start justify-between gap-4 border-b border-[var(--panel-border)] px-6 py-4">
          <h2 className="text-lg font-semibold tracking-[-0.02em] text-[var(--foreground)]">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            aria-label={dictionary.common.close}
            className="rounded-full px-2 py-1 text-sm text-[var(--muted)] transition hover:text-[var(--foreground)]"
          >
            ×
          </button>
        </div>
        <div className="max-h-[calc(100vh-8rem)] overflow-y-auto px-6 py-5">{children}</div>
      </div>
    </div>,
    document.body,
  );
}
