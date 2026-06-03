import { apiDelete, apiFetch, apiMutate, type ApiResult } from "@/lib/api/client";
import type { ApiAdvertiser, ApiCampaign, Campaign, CampaignStatus } from "@/lib/types";

const campaignsEndpoint = "/api/campaigns";
const advertisersEndpoint = "/api/advertisers";

type ApiCampaignCreative = {
  id: string;
  campaignId: string;
  creativeId: string;
  createdAt: string;
};

export type { ApiCampaignCreative };

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

export type { ApiDeliveryConstraints };

export type UpsertDeliveryConstraintsPayload = {
  cities: string[];
  buildingTypes: string[];
  screenOrientations: string[];
  daysOfWeek: string[];
  startTime: string | null;
  endTime: string | null;
};

export async function getDeliveryConstraints(
  campaignId: string,
): Promise<ApiResult<ApiDeliveryConstraints>> {
  return apiFetch<ApiDeliveryConstraints>(`${campaignsEndpoint}/${campaignId}/delivery-constraints`);
}

export async function upsertDeliveryConstraints(
  campaignId: string,
  payload: UpsertDeliveryConstraintsPayload,
): Promise<ApiResult<ApiDeliveryConstraints>> {
  return apiMutate<UpsertDeliveryConstraintsPayload, ApiDeliveryConstraints>(
    `${campaignsEndpoint}/${campaignId}/delivery-constraints`,
    "PUT",
    payload,
  );
}

type CampaignExtras = {
  id: string;
  creatives: number;
  deliveryConstraints: string;
};

export type CreateCampaignPayload = {
  advertiserId: string;
  name: string;
  startDate: string | null;
  endDate: string | null;
  status: string;
  dailyBudget: number | null;
  totalBudget: number | null;
  maxCpm: number | null;
};

export type UpdateCampaignPayload = Omit<CreateCampaignPayload, "advertiserId">;

export async function createCampaign(payload: CreateCampaignPayload): Promise<ApiResult<ApiCampaign>> {
  return apiMutate<CreateCampaignPayload, ApiCampaign>(campaignsEndpoint, "POST", payload);
}

export async function updateCampaign(
  id: string,
  payload: UpdateCampaignPayload,
): Promise<ApiResult<ApiCampaign>> {
  return apiMutate<UpdateCampaignPayload, ApiCampaign>(`${campaignsEndpoint}/${id}`, "PUT", payload);
}

export async function getCampaignsList(): Promise<ApiResult<ApiCampaign[]>> {
  return apiFetch<ApiCampaign[]>(campaignsEndpoint);
}

export async function getCampaignCreatives(
  campaignId: string,
): Promise<ApiResult<ApiCampaignCreative[]>> {
  return apiFetch<ApiCampaignCreative[]>(`${campaignsEndpoint}/${campaignId}/creatives`);
}

export async function assignCreative(
  campaignId: string,
  creativeId: string,
): Promise<ApiResult<ApiCampaignCreative>> {
  return apiMutate<Record<string, never>, ApiCampaignCreative>(
    `${campaignsEndpoint}/${campaignId}/creatives/${creativeId}`,
    "POST",
    {},
  );
}

export async function removeCreative(campaignId: string, creativeId: string): Promise<ApiResult<void>> {
  return apiDelete(`${campaignsEndpoint}/${campaignId}/creatives/${creativeId}`);
}

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

  let constraints: ApiDeliveryConstraints | null = null;

  if (constraintsResult.ok) {
    constraints = constraintsResult.data;
  } else if (constraintsResult.status !== 404) {
    return constraintsResult;
  }

  return {
    ok: true,
    data: {
      id: campaignId,
      creatives: creativesResult.data.length,
      deliveryConstraints: constraints ? formatDeliveryConstraints(constraints) : "All",
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
