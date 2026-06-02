import { apiFetch, type ApiResult } from "@/lib/api/client";
import type { Advertiser, ApiAdvertiser, ApiCampaign, EntityStatus } from "@/lib/types";

const advertisersEndpoint = "/api/advertisers";
const campaignsEndpoint = "/api/campaigns";

export async function getAdvertisers(): Promise<ApiResult<Advertiser[]>> {
  const [advertisersResult, campaignsResult] = await Promise.all([
    apiFetch<ApiAdvertiser[]>(advertisersEndpoint),
    apiFetch<ApiCampaign[]>(campaignsEndpoint),
  ]);

  if (!advertisersResult.ok) {
    return advertisersResult;
  }

  if (!campaignsResult.ok) {
    return campaignsResult;
  }

  const campaignCounts = new Map<string, number>();
  for (const campaign of campaignsResult.data) {
    campaignCounts.set(campaign.advertiserId, (campaignCounts.get(campaign.advertiserId) ?? 0) + 1);
  }

  return {
    ok: true,
    data: advertisersResult.data.map((advertiser) => mapAdvertiser(advertiser, campaignCounts)),
  };
}

function mapAdvertiser(advertiser: ApiAdvertiser, campaignCounts: Map<string, number>): Advertiser {
  return {
    id: advertiser.id,
    name: advertiser.name,
    contact: advertiser.contactName,
    email: advertiser.contactEmail,
    status: normalizeEntityStatus(advertiser.status),
    campaigns: campaignCounts.get(advertiser.id) ?? 0,
  };
}

function normalizeEntityStatus(status: string): EntityStatus {
  return status === "Inactive" ? "Inactive" : "Active";
}
