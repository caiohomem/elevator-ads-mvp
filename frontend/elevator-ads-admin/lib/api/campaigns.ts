import { apiFetch, type ApiResult } from "@/lib/api/client";
import type { ApiAdvertiser, ApiCampaign, Campaign, CampaignStatus } from "@/lib/types";

const campaignsEndpoint = "/api/campaigns";
const advertisersEndpoint = "/api/advertisers";

type ApiCampaignCreative = {
  id: string;
  campaignId: string;
  creativeId: string;
  createdAt: string;
};

type ApiDeliveryConstraints = {
  id: string;
  campaignId: string;
  cities: string[];
  buildingTypes: string[];
  screenOrientations: string[];
  daysOfWeek: string[];
  startTime: string | null;
  endTime: string | null;
  createdAt: string;
  updatedAt: string;
};

type CampaignExtras = {
  id: string;
  creatives: number;
  deliveryConstraints: string;
};

export async function getCampaigns(): Promise<ApiResult<Campaign[]>> {
  const [campaignsResult, advertisersResult] = await Promise.all([
    apiFetch<ApiCampaign[]>(campaignsEndpoint),
    apiFetch<ApiAdvertiser[]>(advertisersEndpoint),
  ]);

  if (!campaignsResult.ok) {
    return campaignsResult;
  }

  if (!advertisersResult.ok) {
    return advertisersResult;
  }

  const advertiserNamesById = new Map(advertisersResult.data.map((advertiser) => [advertiser.id, advertiser.name] as const));
  const extras = await Promise.all(campaignsResult.data.map(async (campaign) => fetchCampaignExtras(campaign.id)));

  const failedExtras = extras.find((item): item is { ok: false; status: number; message: string } => !item.ok);
  if (failedExtras) {
    return failedExtras;
  }

  const successfulExtras = extras as Array<{ ok: true; data: CampaignExtras }>;
  const extrasByCampaignId = new Map(successfulExtras.map((item) => [item.data.id, item.data] as const));

  return {
    ok: true,
    data: campaignsResult.data.map((campaign) => mapCampaign(campaign, advertiserNamesById, extrasByCampaignId)),
  };
}

async function fetchCampaignExtras(campaignId: string): Promise<ApiResult<CampaignExtras>> {
  const [creativesResult, constraintsResult] = await Promise.all([
    apiFetch<ApiCampaignCreative[]>(`${campaignsEndpoint}/${campaignId}/creatives`),
    apiFetch<ApiDeliveryConstraints>(`${campaignsEndpoint}/${campaignId}/delivery-constraints`),
  ]);

  if (!creativesResult.ok) {
    return creativesResult;
  }

  if (!constraintsResult.ok) {
    return constraintsResult;
  }

  return {
    ok: true,
    data: {
      id: campaignId,
      creatives: creativesResult.data.length,
      deliveryConstraints: formatDeliveryConstraints(constraintsResult.data),
    },
  };
}

function mapCampaign(
  campaign: ApiCampaign,
  advertiserNamesById: Map<string, string>,
  extrasByCampaignId: Map<string, CampaignExtras>,
): Campaign {
  const extras = extrasByCampaignId.get(campaign.id);

  return {
    id: campaign.id,
    name: campaign.name,
    advertiserId: campaign.advertiserId,
    advertiserName: advertiserNamesById.get(campaign.advertiserId) ?? campaign.advertiserId,
    status: normalizeCampaignStatus(campaign.status),
    startDate: formatDate(campaign.startDate),
    endDate: formatDate(campaign.endDate),
    dailyBudget: Number(campaign.dailyBudget ?? 0),
    creatives: extras?.creatives ?? 0,
    deliveryConstraints: extras?.deliveryConstraints ?? "",
  };
}

function normalizeCampaignStatus(status: string): CampaignStatus {
  if (status === "Draft" || status === "Scheduled" || status === "Active" || status === "Paused") {
    return status;
  }

  return "Draft";
}

function formatDate(value: string | null) {
  return value?.slice(0, 10) ?? "-";
}

function formatDeliveryConstraints(constraints: ApiDeliveryConstraints) {
  const parts: string[] = [];

  if (constraints.cities.length > 0) {
    parts.push(`Cities: ${constraints.cities.join(", ")}`);
  }

  if (constraints.buildingTypes.length > 0) {
    parts.push(`Building types: ${constraints.buildingTypes.join(", ")}`);
  }

  if (constraints.screenOrientations.length > 0) {
    parts.push(`Orientations: ${constraints.screenOrientations.join(", ")}`);
  }

  if (constraints.daysOfWeek.length > 0) {
    parts.push(`Days: ${constraints.daysOfWeek.join(", ")}`);
  }

  if (constraints.startTime || constraints.endTime) {
    parts.push(`Time: ${constraints.startTime ?? "-"} to ${constraints.endTime ?? "-"}`);
  }

  return parts.length > 0 ? parts.join(" | ") : "All";
}
