import { apiDelete, apiFetch, apiFetchPaged, apiMutate, type ApiResult } from "@/lib/api/client";
import type {
  ApiInventoryPackage,
  ApiScreen,
  InventoryPackageStatus,
  PagedQuery,
  PagedResult,
} from "@/lib/types";

const inventoryPackagesEndpoint = "/api/inventory-packages";

export type InventoryPackagePayload = {
  name: string;
  description: string;
  cities: string[];
  buildingTypes: string[];
  screenOrientations: string[];
  screenIds: string[];
  buildingIds: string[];
  baseCpm: number;
  status: InventoryPackageStatus;
};

export async function getInventoryPackagesPaged(
  query: PagedQuery,
): Promise<ApiResult<PagedResult<ApiInventoryPackage>>> {
  return apiFetchPaged<ApiInventoryPackage>(inventoryPackagesEndpoint, query);
}

export async function getInventoryPackageById(id: string): Promise<ApiResult<ApiInventoryPackage>> {
  return apiFetch<ApiInventoryPackage>(`${inventoryPackagesEndpoint}/${id}`);
}

export async function createInventoryPackage(
  payload: InventoryPackagePayload,
): Promise<ApiResult<ApiInventoryPackage>> {
  return apiMutate<InventoryPackagePayload, ApiInventoryPackage>(inventoryPackagesEndpoint, "POST", payload);
}

export async function updateInventoryPackage(
  id: string,
  payload: InventoryPackagePayload,
): Promise<ApiResult<ApiInventoryPackage>> {
  return apiMutate<InventoryPackagePayload, ApiInventoryPackage>(
    `${inventoryPackagesEndpoint}/${id}`,
    "PUT",
    payload,
  );
}

export async function deleteInventoryPackage(id: string): Promise<ApiResult<void>> {
  return apiDelete(`${inventoryPackagesEndpoint}/${id}`);
}

export async function getInventoryPackageScreens(id: string): Promise<ApiResult<ApiScreen[]>> {
  return apiFetch<ApiScreen[]>(`${inventoryPackagesEndpoint}/${id}/screens`);
}
