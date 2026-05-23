"use client";

import { ClientPageFrame } from "@/components/ClientPageFrame";
import { SummaryCard } from "@/components/SummaryCard";
import { useTranslation } from "@/lib/i18n";

export default function ReportsPage() {
  const { dictionary } = useTranslation();

  return (
    <ClientPageFrame section="reports">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        <SummaryCard
          label={dictionary.reports.playsToday}
          value="128,742"
          description="Aggregate mocked plays for the current operating day."
        />
        <SummaryCard
          label={dictionary.reports.playsByCampaign}
          value="9 active"
          description="Campaign-level playback rollups will land here next."
        />
        <SummaryCard
          label={dictionary.reports.playsByScreen}
          value="86 screens"
          description="Screen drill-down placeholder for future operational reporting."
        />
        <SummaryCard
          label={dictionary.reports.pendingProofOfPlay}
          value="7 pending"
          description="Proof-of-play reconciliation remains a later integration."
        />
      </div>
    </ClientPageFrame>
  );
}
