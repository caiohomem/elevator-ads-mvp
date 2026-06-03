import { apiFetch, apiMutate, logDependentEndpointWarning, type ApiResult } from "@/lib/api/client";
import type { ApiBuilding, ApiScreen, Building, BuildingType } from "@/lib/types";

const buildingsEndpoint = "/api/buildings";
const screensEndpoint = "/api/screens";

export type CreateBuildingPayload = {
  name: string;
  address: string;
  city: string;
  country: string;
  postalCode: string;
  buildingType: string;
  estimatedDailyAudience: number;
};

export async function createBuilding(
  payload: CreateBuildingPayload,
): Promise<ApiResult<ApiBuilding>> {
  return apiMutate<CreateBuildingPayload, ApiBuilding>(buildingsEndpoint, "POST", payload);
}

export async function updateBuilding(
  id: string,
  payload: CreateBuildingPayload,
): Promise<ApiResult<ApiBuilding>> {
  return apiMutate<CreateBuildingPayload, ApiBuilding>(`${buildingsEndpoint}/${id}`, "PUT", payload);
}

export async function getBuildings(): Promise<ApiResult<Building[]>> {
  const [buildingsResult, screensResult] = await Promise.all([
    apiFetch<ApiBuilding[]>(buildingsEndpoint),
    apiFetch<ApiScreen[]>(screensEndpoint),
  ]);

  if (!buildingsResult.ok) {
    return buildingsResult;
  }

  if (!screensResult.ok) {
    logDependentEndpointWarning(screensEndpoint, screensResult, "building screen counts = 0 and status = Inactive");
  }

  if (!screensResult.ok) {
    return {
      ok: true,
      data: buildingsResult.data.map((building) => mapBuilding(building, new Map(), new Map())),
    };
  }

  const screenCounts = new Map<string, number>();
  const hasActiveScreens = new Map<string, boolean>();

  for (const screen of screensResult.data) {
    screenCounts.set(screen.buildingId, (screenCounts.get(screen.buildingId) ?? 0) + 1);
    if (screen.status === "Active") {
      hasActiveScreens.set(screen.buildingId, true);
    }
  }

  return {
    ok: true,
    data: buildingsResult.data.map((building) => mapBuilding(building, screenCounts, hasActiveScreens)),
  };
}

export async function getBuildingsList(): Promise<ApiResult<ApiBuilding[]>> {
  return apiFetch<ApiBuilding[]>(buildingsEndpoint);
}

function mapBuilding(
  building: ApiBuilding,
  screenCounts: Map<string, number>,
  hasActiveScreens: Map<string, boolean>,
): Building {
  return {
    id: building.id,
    name: building.name,
    city: building.city,
    country: building.country,
    type: normalizeBuildingType(building.buildingType),
    estimatedDailyAudience: building.estimatedDailyAudience,
    screens: screenCounts.get(building.id) ?? 0,
    status: hasActiveScreens.get(building.id) ? "Active" : "Inactive",
  };
}

function normalizeBuildingType(type: string): BuildingType {
  if (type === "Residential" || type === "Commercial" || type === "MixedUse" || type === "Hospitality") {
    return type;
  }

  if (type === "Corporate") {
    return "Commercial";
  }

  return "MixedUse";
}
