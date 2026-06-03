import { apiFetch, apiMutate, type ApiResource, type ApiResult, withMockFallback } from "@/lib/api/client";
import { getCampaignsList } from "@/lib/api/campaigns";
import { dailyPlaylists as mockPlaylists } from "@/lib/mockData";
import type {
  ApiBuilding,
  ApiCampaign,
  ApiCreative,
  ApiDailyPlaylist,
  ApiDailyPlaylistItem,
  ApiScreen,
  DailyPlaylist,
  DailyPlaylistItem,
  PlaylistStatus,
} from "@/lib/types";

const playlistsEndpoint = "/api/playlists";
const screensEndpoint = "/api/screens";
const buildingsEndpoint = "/api/buildings";
const creativesEndpoint = "/api/creatives";

type PlaylistLookups = {
  screenNamesById: Map<string, string>;
  buildingIdByScreenId: Map<string, string>;
  buildingNamesById: Map<string, string>;
  creativeNamesById: Map<string, string>;
  creativeMediaTypesById: Map<string, string>;
  campaignNamesById: Map<string, string>;
};

export async function getDailyPlaylists(): Promise<ApiResource<DailyPlaylist[]>> {
  const [playlistsResult, lookupsResult] = await Promise.all([apiFetch<ApiDailyPlaylist[]>(playlistsEndpoint), loadLookups()]);

  if (!playlistsResult.ok) {
    return withMockFallback(playlistsEndpoint, playlistsResult, mockPlaylists);
  }

  return {
    ok: true,
    data: playlistsResult.data.map((playlist) => mapPlaylist(playlist, lookupsResult)),
  };
}

export async function getDailyPlaylistsList(): Promise<ApiResult<ApiDailyPlaylist[]>> {
  return apiFetch<ApiDailyPlaylist[]>(playlistsEndpoint);
}

export async function getDailyPlaylistById(id: string): Promise<ApiResult<DailyPlaylist>> {
  const [playlistResult, lookupsResult] = await Promise.all([
    apiFetch<ApiDailyPlaylist>(`${playlistsEndpoint}/${id}`),
    loadLookups(),
  ]);

  if (!playlistResult.ok) {
    return playlistResult;
  }

  return { ok: true, data: mapPlaylist(playlistResult.data, lookupsResult) };
}

export async function generatePlaylists(date: string): Promise<ApiResult<ApiDailyPlaylist[]>> {
  return apiMutate<Record<string, never>, ApiDailyPlaylist[]>(
    `${playlistsEndpoint}/generate?date=${encodeURIComponent(date)}`,
    "POST",
    {},
  );
}

export async function publishPlaylist(id: string): Promise<ApiResult<ApiDailyPlaylist>> {
  return apiMutate<Record<string, never>, ApiDailyPlaylist>(`${playlistsEndpoint}/${id}/publish`, "POST", {});
}

async function loadLookups(): Promise<PlaylistLookups> {
  const [screensResult, buildingsResult, creativesResult, campaignsResult] = await Promise.all([
    apiFetch<ApiScreen[]>(screensEndpoint),
    apiFetch<ApiBuilding[]>(buildingsEndpoint),
    apiFetch<ApiCreative[]>(creativesEndpoint),
    getCampaignsList(),
  ]);

  const screensData: ApiScreen[] = screensResult.ok ? screensResult.data : [];
  const buildingsData: ApiBuilding[] = buildingsResult.ok ? buildingsResult.data : [];
  const creativesData: ApiCreative[] = creativesResult.ok ? creativesResult.data : [];
  const campaignsData: ApiCampaign[] = campaignsResult.ok ? campaignsResult.data : [];

  return {
    screenNamesById: new Map(screensData.map((screen) => [screen.id, screen.name] as const)),
    buildingIdByScreenId: new Map(screensData.map((screen) => [screen.id, screen.buildingId] as const)),
    buildingNamesById: new Map(buildingsData.map((building) => [building.id, building.name] as const)),
    creativeNamesById: new Map(creativesData.map((creative) => [creative.id, creative.name] as const)),
    creativeMediaTypesById: new Map(creativesData.map((creative) => [creative.id, creative.mediaType] as const)),
    campaignNamesById: new Map(campaignsData.map((campaign) => [campaign.id, campaign.name] as const)),
  };
}

function mapPlaylist(playlist: ApiDailyPlaylist, lookups: PlaylistLookups): DailyPlaylist {
  const screenName = lookups.screenNamesById.get(playlist.screenId) ?? playlist.screenId;
  const buildingId = lookups.buildingIdByScreenId.get(playlist.screenId);
  const buildingName = buildingId ? (lookups.buildingNamesById.get(buildingId) ?? buildingId) : "-";

  return {
    id: playlist.id,
    date: playlist.date,
    screenId: playlist.screenId,
    screenName,
    buildingName,
    version: `v${playlist.version}`,
    status: normalizePlaylistStatus(playlist.status),
    items: playlist.items
      .map((item) => mapPlaylistItem(item, lookups))
      .sort((a, b) => a.position - b.position),
    generatedAt: formatDateTime(playlist.generatedAt),
    publishedAt: playlist.publishedAt ? formatDateTime(playlist.publishedAt) : null,
    downloadedAt: null,
  };
}

function mapPlaylistItem(item: ApiDailyPlaylistItem, lookups: PlaylistLookups): DailyPlaylistItem {
  return {
    id: item.id,
    creativeName: lookups.creativeNamesById.get(item.creativeId) ?? item.creativeId,
    campaignName: lookups.campaignNamesById.get(item.campaignId) ?? item.campaignId,
    mediaType: lookups.creativeMediaTypesById.get(item.creativeId) ?? "-",
    position: item.order,
    durationSeconds: item.durationSeconds,
  };
}

function normalizePlaylistStatus(status: string): PlaylistStatus {
  if (status === "Draft" || status === "Published" || status === "Downloaded" || status === "Expired") {
    return status;
  }

  return "Draft";
}

function formatDateTime(value: string) {
  return value.replace("T", " ").slice(0, 16);
}
