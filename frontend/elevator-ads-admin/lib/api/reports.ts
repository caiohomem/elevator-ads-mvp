import { apiFetch, apiFetchPaged, type ApiResult } from "@/lib/api/client";
import type {
  CampaignReport,
  OverviewReport,
  ProofOfPlayEvent,
  PagedQuery,
  PagedResult,
  ScreenReport,
} from "@/lib/types";

const reportsEndpoint = "/api/reports";
const playbackReportsEndpoint = "/api/playback-reports";

export async function getReportsOverview(date: string): Promise<ApiResult<OverviewReport>> {
  return apiFetch<OverviewReport>(`${reportsEndpoint}/overview?date=${encodeURIComponent(date)}`);
}

export async function getReportsCampaigns(
  from: string,
  to: string,
): Promise<ApiResult<CampaignReport>> {
  return apiFetch<CampaignReport>(
    `${reportsEndpoint}/campaigns?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
  );
}

export async function getReportsScreens(
  from: string,
  to: string,
): Promise<ApiResult<ScreenReport>> {
  return apiFetch<ScreenReport>(
    `${reportsEndpoint}/screens?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
  );
}

export async function getProofOfPlayEvents(): Promise<ApiResult<ProofOfPlayEvent[]>> {
  return apiFetch<ProofOfPlayEvent[]>(`${playbackReportsEndpoint}/`);
}

export async function getProofOfPlayEventsPaged(
  query: PagedQuery,
): Promise<ApiResult<PagedResult<ProofOfPlayEvent>>> {
  return apiFetchPaged<ProofOfPlayEvent>(`${playbackReportsEndpoint}/`, query);
}
