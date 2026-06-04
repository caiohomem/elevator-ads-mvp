import { apiFetchList, apiFetchPaged, apiMutate, type ApiResult } from "@/lib/api/client";
import type {
  Advertiser,
  ApiAdvertiser,
  ApiCampaign,
  EntityStatus,
  PagedQuery,
  PagedResult,
} from "@/lib/types";

const advertisersEndpoint = "/api/advertisers";
const campaignsEndpoint = "/api/campaigns";

export type CreateAdvertiserPayload = {
  name: string;
  legalName: string;
  taxId: string;
  contactName: string;
  contactEmail: string;
  phone: string;
  status: string;
};

export type UpdateAdvertiserPayload = CreateAdvertiserPayload;

export async function createAdvertiser(
  payload: CreateAdvertiserPayload,
): Promise<ApiResult<ApiAdvertiser>> {
  return apiMutate<CreateAdvertiserPayload, ApiAdvertiser>(advertisersEndpoint, "POST", payload);
}

export async function updateAdvertiser(
  id: string,
  payload: UpdateAdvertiserPayload,
): Promise<ApiResult<ApiAdvertiser>> {
  return apiMutate<UpdateAdvertiserPayload, ApiAdvertiser>(`${advertisersEndpoint}/${id}`, "PUT", payload);
}

export async function getAdvertisers(): Promise<ApiResult<Advertiser[]>> {
  const [advertisersResult, campaignsResult] = await Promise.all([
    apiFetchList<ApiAdvertiser>(advertisersEndpoint),
    apiFetchList<ApiCampaign>(campaignsEndpoint),
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

export async function getAdvertisersList(): Promise<ApiResult<ApiAdvertiser[]>> {
  return apiFetchList<ApiAdvertiser>(advertisersEndpoint);
}

export async function getAdvertisersPaged(
  query: PagedQuery,
): Promise<ApiResult<PagedResult<ApiAdvertiser>>> {
  return apiFetchPaged<ApiAdvertiser>(advertisersEndpoint, query);
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
