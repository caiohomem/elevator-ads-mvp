import { apiFetchList, apiMutate, type ApiResult } from "@/lib/api/client";
import type { ApiAdvertiserApiKey, CreateApiKeyPayload, CreateApiKeyResponse } from "@/lib/types";

export async function getAdvertiserApiKeys(advertiserId: string): Promise<ApiResult<ApiAdvertiserApiKey[]>> {
  return apiFetchList<ApiAdvertiserApiKey>(`/api/advertisers/${advertiserId}/api-keys`);
}

export async function createAdvertiserApiKey(
  advertiserId: string,
  payload: CreateApiKeyPayload,
): Promise<ApiResult<CreateApiKeyResponse>> {
  return apiMutate<CreateApiKeyPayload, CreateApiKeyResponse>(`/api/advertisers/${advertiserId}/api-keys`, "POST", payload);
}

export async function revokeAdvertiserApiKey(
  advertiserId: string,
  apiKeyId: string,
): Promise<ApiResult<ApiAdvertiserApiKey>> {
  return apiMutate<Record<string, never>, ApiAdvertiserApiKey>(
    `/api/advertisers/${advertiserId}/api-keys/${apiKeyId}/revoke`,
    "POST",
    {},
  );
}
