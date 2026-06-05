import { apiFetch, type ApiResult } from "@/lib/api/client";
import type { AdvertiserCampaignReport } from "@/lib/types";

export async function getAdvertiserCampaignReport(
  advertiserId: string,
  campaignId: string,
  from: string,
  to: string,
): Promise<ApiResult<AdvertiserCampaignReport>> {
  return apiFetch<AdvertiserCampaignReport>(
    `/api/advertisers/${encodeURIComponent(advertiserId)}/campaign-reports/${encodeURIComponent(campaignId)}?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
  );
}
