import { apiFetch, type ApiResult } from "@/lib/api/client";
import type { EstimatedProofOfPlayReport } from "@/lib/types";

const reportsEndpoint = "/api/reports";

export async function getEstimatedProofOfPlay(
  campaignId: string,
  from: string,
  to: string,
): Promise<ApiResult<EstimatedProofOfPlayReport>> {
  return apiFetch<EstimatedProofOfPlayReport>(
    `${reportsEndpoint}/estimated-proof-of-play?campaignId=${encodeURIComponent(campaignId)}&from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
  );
}
