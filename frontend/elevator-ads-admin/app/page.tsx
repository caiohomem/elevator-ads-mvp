"use client";

import { SummaryCard } from "@/components/SummaryCard";
import { getDashboardSummary, getRecentActivity } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import type { ActivityEvent, DashboardSummary } from "@/lib/types";
import { useEffect, useState } from "react";

const cardDescriptions = {
  buildings: "Live inventory across elevator-enabled properties.",
  screens: "Total registered digital displays.",
  activeScreens: "Online screens currently available for delivery.",
  advertisers: "Accounts with approved or draft campaigns.",
  activeCampaigns: "Campaigns within an active flight window.",
  playlistsGeneratedToday: "Published daily sequences prepared today.",
  playlistsPendingDownload: "Published packages not yet fetched by players.",
  playsReportedToday: "Mocked proof-of-play events processed today.",
} as const;

export default function Home() {
  const { dictionary } = useTranslation();
  const [dashboardSummary, setDashboardSummary] = useState<DashboardSummary | null>(null);
  const [recentActivity, setRecentActivity] = useState<ActivityEvent[]>([]);

  useEffect(() => {
    let active = true;

    Promise.all([getDashboardSummary(), getRecentActivity()]).then(([summary, activity]) => {
      if (!active) {
        return;
      }

      setDashboardSummary(summary);
      setRecentActivity(activity);
    });

    return () => {
      active = false;
    };
  }, []);

  if (!dashboardSummary) {
    return (
      <section className="space-y-6">
        <div className="panel rounded-[32px] px-6 py-7 text-sm font-semibold text-[var(--foreground)] sm:px-8">
          {dictionary.common.loading}
        </div>
      </section>
    );
  }

  const cards = [
    ["buildings", dashboardSummary.buildings],
    ["screens", dashboardSummary.screens],
    ["activeScreens", dashboardSummary.activeScreens],
    ["advertisers", dashboardSummary.advertisers],
    ["activeCampaigns", dashboardSummary.activeCampaigns],
    ["playlistsGeneratedToday", dashboardSummary.playlistsGeneratedToday],
    ["playlistsPendingDownload", dashboardSummary.playlistsPendingDownload],
    ["playsReportedToday", dashboardSummary.playsReportedToday.toLocaleString("en-US")],
  ] as const;

  return (
    <section className="space-y-6">
      <div className="panel relative overflow-hidden rounded-[32px] px-6 py-7 sm:px-8">
        <div className="absolute inset-y-0 right-0 hidden w-1/3 bg-[radial-gradient(circle_at_center,rgba(199,103,47,0.22),transparent_58%)] lg:block" />
        <p className="text-[0.7rem] font-semibold uppercase tracking-[0.3em] text-[var(--accent)]">
          Daily DOOH control
        </p>
        <h1 className="mt-3 max-w-3xl text-4xl font-semibold tracking-[-0.06em] text-[var(--foreground)] sm:text-5xl">
          {dictionary.dashboard.title}
        </h1>
        <p className="mt-4 max-w-3xl text-sm leading-7 text-[var(--muted)] sm:text-base">
          {dictionary.dashboard.description}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {cards.map(([key, value]) => (
          <SummaryCard
            key={key}
            label={dictionary.dashboard.cards[key]}
            value={value}
            description={cardDescriptions[key]}
          />
        ))}
      </div>

      <div className="panel rounded-[32px] p-6 sm:p-7">
        <div className="mb-5">
          <p className="text-[0.7rem] font-semibold uppercase tracking-[0.26em] text-[var(--accent)]">
            Timeline
          </p>
          <h2 className="mt-2 text-2xl font-semibold tracking-[-0.05em] text-[var(--foreground)]">
            {dictionary.common.recentActivity}
          </h2>
        </div>

        <div className="space-y-3">
          {recentActivity.map((event) => (
            <div
              key={event.id}
              className="rounded-[24px] border border-[var(--panel-border)] bg-white/25 px-4 py-4 dark:bg-white/[0.03]"
            >
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-sm font-semibold text-[var(--foreground)]">{event.title}</p>
                  <p className="mt-1 text-sm leading-6 text-[var(--muted)]">{event.detail}</p>
                </div>
                <span className="font-mono text-xs text-[var(--muted)]">{event.time}</span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
