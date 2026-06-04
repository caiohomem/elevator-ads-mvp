import { apiFetchList, apiFetchPaged, apiMutate, type ApiResult } from "@/lib/api/client";
import type {
  ApiAdvertiser,
  ApiCreative,
  ApprovalStatus,
  Creative,
  PagedQuery,
  PagedResult,
} from "@/lib/types";

const creativesEndpoint = "/api/creatives";
const advertisersEndpoint = "/api/advertisers";

export type CreateCreativePayload = {
  advertiserId: string;
  name: string;
  mediaUrl: string;
  mediaType: string;
  durationSeconds: number;
};

export type UpdateCreativePayload = {
  name: string;
  mediaUrl: string;
  mediaType: string;
  durationSeconds: number;
};

export async function createCreative(
  payload: CreateCreativePayload,
): Promise<ApiResult<ApiCreative>> {
  return apiMutate<CreateCreativePayload, ApiCreative>(creativesEndpoint, "POST", payload);
}

export async function updateCreative(
  id: string,
  payload: UpdateCreativePayload,
): Promise<ApiResult<ApiCreative>> {
  return apiMutate<UpdateCreativePayload, ApiCreative>(`${creativesEndpoint}/${id}`, "PUT", payload);
}

export async function submitCreativeForReview(id: string): Promise<ApiResult<ApiCreative>> {
  return apiMutate<Record<string, never>, ApiCreative>(
    `${creativesEndpoint}/${id}/submit-for-review`,
    "POST",
    {},
  );
}

export async function approveCreative(id: string): Promise<ApiResult<ApiCreative>> {
  return apiMutate<Record<string, never>, ApiCreative>(`${creativesEndpoint}/${id}/approve`, "POST", {});
}

export async function rejectCreative(id: string): Promise<ApiResult<ApiCreative>> {
  return apiMutate<Record<string, never>, ApiCreative>(`${creativesEndpoint}/${id}/reject`, "POST", {});
}

export async function getCreatives(): Promise<ApiResult<Creative[]>> {
  const [creativesResult, advertisersResult] = await Promise.all([
    apiFetchList<ApiCreative>(creativesEndpoint),
    apiFetchList<ApiAdvertiser>(advertisersEndpoint),
  ]);

  if (!creativesResult.ok) {
    return creativesResult;
  }

  if (!advertisersResult.ok) {
    return advertisersResult;
  }

  const advertisersById = new Map(advertisersResult.data.map((advertiser) => [advertiser.id, advertiser.name] as const));

  return {
    ok: true,
    data: creativesResult.data.map((creative) => mapCreative(creative, advertisersById)),
  };
}

export async function getCreativesList(): Promise<ApiResult<ApiCreative[]>> {
  return apiFetchList<ApiCreative>(creativesEndpoint);
}

export async function getCreativesPaged(
  query: PagedQuery,
): Promise<ApiResult<PagedResult<ApiCreative>>> {
  return apiFetchPaged<ApiCreative>(creativesEndpoint, query);
}

function mapCreative(creative: ApiCreative, advertisersById: Map<string, string>): Creative {
  return {
    id: creative.id,
    name: creative.name,
    advertiserId: creative.advertiserId,
    advertiserName: advertisersById.get(creative.advertiserId) ?? creative.advertiserId,
    mediaType: creative.mediaType === "Video" ? "Video" : "Image",
    durationSeconds: creative.durationSeconds,
    approvalStatus: normalizeApprovalStatus(creative.approvalStatus),
  };
}

function normalizeApprovalStatus(status: string): ApprovalStatus {
  if (status === "Draft" || status === "PendingReview" || status === "Approved" || status === "Rejected") {
    return status;
  }

  return "Draft";
}
