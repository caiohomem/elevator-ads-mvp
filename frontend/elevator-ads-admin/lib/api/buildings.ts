import { apiFetch, type ApiResult } from "@/lib/api/client";
import type { ApiBuilding, ApiScreen, Building, BuildingType } from "@/lib/types";

const buildingsEndpoint = "/api/buildings";
const screensEndpoint = "/api/screens";

export async function getBuildings(): Promise<ApiResult<Building[]>> {
  const [buildingsResult, screensResult] = await Promise.all([
    apiFetch<ApiBuilding[]>(buildingsEndpoint),
    apiFetch<ApiScreen[]>(screensEndpoint),
  ]);

  if (!buildingsResult.ok) {
    return buildingsResult;
  }

  if (!screensResult.ok) {
    return screensResult;
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
