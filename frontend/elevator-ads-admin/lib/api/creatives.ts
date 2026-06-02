import { apiFetch, type ApiResult } from "@/lib/api/client";
import type { ApiAdvertiser, ApiCreative, ApprovalStatus, Creative } from "@/lib/types";

const creativesEndpoint = "/api/creatives";
const advertisersEndpoint = "/api/advertisers";

export async function getCreatives(): Promise<ApiResult<Creative[]>> {
  const [creativesResult, advertisersResult] = await Promise.all([
    apiFetch<ApiCreative[]>(creativesEndpoint),
    apiFetch<ApiAdvertiser[]>(advertisersEndpoint),
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
