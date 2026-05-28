import { apiFetch, type ApiResource, withMockFallback } from "@/lib/api/client";
import { dailyPlaylists as mockPlaylists } from "@/lib/mockData";
import type {
  ApiBuilding,
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

export async function getDailyPlaylists(): Promise<ApiResource<DailyPlaylist[]>> {
  const [playlistsResult, screensResult, buildingsResult, creativesResult] = await Promise.all([
    apiFetch<ApiDailyPlaylist[]>(playlistsEndpoint),
    apiFetch<ApiScreen[]>(screensEndpoint),
    apiFetch<ApiBuilding[]>(buildingsEndpoint),
    apiFetch<ApiCreative[]>(creativesEndpoint),
  ]);

  if (!playlistsResult.ok) {
    return withMockFallback(playlistsEndpoint, playlistsResult, mockPlaylists);
  }

  if (!screensResult.ok) {
    return withMockFallback(screensEndpoint, screensResult, mockPlaylists);
  }

  if (!buildingsResult.ok) {
    return withMockFallback(buildingsEndpoint, buildingsResult, mockPlaylists);
  }

  if (!creativesResult.ok) {
    return withMockFallback(creativesEndpoint, creativesResult, mockPlaylists);
  }

  const screenNamesById = new Map(screensResult.data.map((screen) => [screen.id, screen.name] as const));
  const buildingIdByScreenId = new Map(screensResult.data.map((screen) => [screen.id, screen.buildingId] as const));
  const buildingNamesById = new Map(buildingsResult.data.map((building) => [building.id, building.name] as const));
  const creativeNamesById = new Map(creativesResult.data.map((creative) => [creative.id, creative.name] as const));

  return {
    data: playlistsResult.data.map((playlist) =>
      mapPlaylist(playlist, screenNamesById, buildingIdByScreenId, buildingNamesById, creativeNamesById),
    ),
  };
}

function mapPlaylist(
  playlist: ApiDailyPlaylist,
  screenNamesById: Map<string, string>,
  buildingIdByScreenId: Map<string, string>,
  buildingNamesById: Map<string, string>,
  creativeNamesById: Map<string, string>,
): DailyPlaylist {
  const screenName = screenNamesById.get(playlist.screenId) ?? playlist.screenId;
  const buildingId = buildingIdByScreenId.get(playlist.screenId);
  const buildingName = buildingId ? (buildingNamesById.get(buildingId) ?? buildingId) : "-";

  return {
    id: playlist.id,
    date: playlist.date,
    screenId: playlist.screenId,
    screenName,
    buildingName,
    version: `v${playlist.version}`,
    status: normalizePlaylistStatus(playlist.status),
    items: playlist.items.map((item) => mapPlaylistItem(item, creativeNamesById)),
    generatedAt: formatDateTime(playlist.generatedAt),
    publishedAt: playlist.publishedAt ? formatDateTime(playlist.publishedAt) : null,
    downloadedAt: null,
  };
}

function mapPlaylistItem(item: ApiDailyPlaylistItem, creativeNamesById: Map<string, string>): DailyPlaylistItem {
  return {
    id: item.id,
    creativeName: creativeNamesById.get(item.creativeId) ?? item.creativeId,
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
