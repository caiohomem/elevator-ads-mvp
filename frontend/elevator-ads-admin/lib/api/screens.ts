import { apiFetch, apiMutate, type ApiResult } from "@/lib/api/client";
import type { ApiBuilding, ApiDailyPlaylist, ApiScreen, Screen, ScreenStatus } from "@/lib/types";

const screensEndpoint = "/api/screens";
const buildingsEndpoint = "/api/buildings";
const playlistsEndpoint = "/api/playlists";

export type CreateScreenPayload = {
  buildingId: string;
  name: string;
  externalCode: string;
  resolutionWidth: number;
  resolutionHeight: number;
  orientation: string;
  status: string;
};

export async function createScreen(
  payload: CreateScreenPayload,
): Promise<ApiResult<ApiScreen>> {
  return apiMutate<CreateScreenPayload, ApiScreen>(screensEndpoint, "POST", payload);
}

export async function updateScreen(
  id: string,
  payload: CreateScreenPayload,
): Promise<ApiResult<ApiScreen>> {
  return apiMutate<CreateScreenPayload, ApiScreen>(`${screensEndpoint}/${id}`, "PUT", payload);
}

export async function getScreens(): Promise<ApiResult<Screen[]>> {
  const [screensResult, buildingsResult, playlistsResult] = await Promise.all([
    apiFetch<ApiScreen[]>(screensEndpoint),
    apiFetch<ApiBuilding[]>(buildingsEndpoint),
    apiFetch<ApiDailyPlaylist[]>(playlistsEndpoint),
  ]);

  if (!screensResult.ok) {
    return screensResult;
  }

  if (!buildingsResult.ok) {
    return buildingsResult;
  }

  if (!playlistsResult.ok) {
    return playlistsResult;
  }

  const buildingsById = new Map(buildingsResult.data.map((building) => [building.id, building.name] as const));
  const latestPlaylistByScreenId = new Map<string, ApiDailyPlaylist>();

  for (const playlist of playlistsResult.data) {
    const current = latestPlaylistByScreenId.get(playlist.screenId);

    if (current === undefined || comparePlaylistRecency(playlist, current) > 0) {
      latestPlaylistByScreenId.set(playlist.screenId, playlist);
    }
  }

  return {
    ok: true,
    data: screensResult.data.map((screen) => mapScreen(screen, buildingsById, latestPlaylistByScreenId)),
  };
}

export async function getScreensList(): Promise<ApiResult<ApiScreen[]>> {
  return apiFetch<ApiScreen[]>(screensEndpoint);
}

function mapScreen(
  screen: ApiScreen,
  buildingsById: Map<string, string>,
  latestPlaylistByScreenId: Map<string, ApiDailyPlaylist>,
): Screen {
  const latestPlaylist = latestPlaylistByScreenId.get(screen.id);

  return {
    id: screen.id,
    name: screen.name,
    buildingId: screen.buildingId,
    buildingName: buildingsById.get(screen.buildingId) ?? screen.buildingId,
    resolution: `${screen.resolutionWidth}x${screen.resolutionHeight}`,
    orientation: screen.orientation === "Landscape" ? "Landscape" : "Portrait",
    status: normalizeScreenStatus(screen.status),
    lastSeen: formatDateTime(screen.lastSeenAt),
    currentPlaylist: latestPlaylist ? `${latestPlaylist.date}-v${latestPlaylist.version}` : "-",
  };
}

function comparePlaylistRecency(left: ApiDailyPlaylist, right: ApiDailyPlaylist) {
  const leftDate = Date.parse(left.date);
  const rightDate = Date.parse(right.date);

  if (leftDate !== rightDate) {
    return leftDate - rightDate;
  }

  return left.version - right.version;
}

function normalizeScreenStatus(status: string): ScreenStatus {
  if (status === "Active" || status === "Inactive" || status === "Offline" || status === "Maintenance") {
    return status;
  }

  return "Offline";
}

function formatDateTime(value: string | null) {
  if (!value) {
    return "-";
  }

  return value.replace("T", " ").slice(0, 16);
}
