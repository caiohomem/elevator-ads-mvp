import { apiFetch, apiFetchPaged, apiMutate, type ApiResult } from "@/lib/api/client";
import type { ApiBookingRequest, PagedQuery, PagedResult } from "@/lib/types";

const bookingRequestsEndpoint = "/api/booking-requests";

export type CreateBookingRequestPayload = {
  advertiserId: string;
  name: string;
  dateFrom: string;
  dateTo: string;
  cities: string[];
  buildingTypes: string[];
  screenOrientations: string[];
  creativeDurationSeconds: number;
  budget: number;
  campaignObjective: string;
  notes: string;
};

export type UpdateBookingRequestPayload = Omit<CreateBookingRequestPayload, "advertiserId">;

export async function getBookingRequestsPaged(
  query: PagedQuery,
): Promise<ApiResult<PagedResult<ApiBookingRequest>>> {
  return apiFetchPaged<ApiBookingRequest>(bookingRequestsEndpoint, query);
}

export async function getBookingRequest(id: string): Promise<ApiResult<ApiBookingRequest>> {
  return apiFetch<ApiBookingRequest>(`${bookingRequestsEndpoint}/${id}`);
}

export async function createBookingRequest(
  payload: CreateBookingRequestPayload,
): Promise<ApiResult<ApiBookingRequest>> {
  return apiMutate<CreateBookingRequestPayload, ApiBookingRequest>(bookingRequestsEndpoint, "POST", payload);
}

export async function updateBookingRequest(
  id: string,
  payload: UpdateBookingRequestPayload,
): Promise<ApiResult<ApiBookingRequest>> {
  return apiMutate<UpdateBookingRequestPayload, ApiBookingRequest>(
    `${bookingRequestsEndpoint}/${id}`,
    "PUT",
    payload,
  );
}

export async function submitBookingRequest(id: string): Promise<ApiResult<ApiBookingRequest>> {
  return apiMutate<Record<string, never>, ApiBookingRequest>(`${bookingRequestsEndpoint}/${id}/submit`, "POST", {});
}

export async function approveBookingRequest(id: string): Promise<ApiResult<ApiBookingRequest>> {
  return apiMutate<Record<string, never>, ApiBookingRequest>(`${bookingRequestsEndpoint}/${id}/approve`, "POST", {});
}

export async function rejectBookingRequest(id: string): Promise<ApiResult<ApiBookingRequest>> {
  return apiMutate<Record<string, never>, ApiBookingRequest>(`${bookingRequestsEndpoint}/${id}/reject`, "POST", {});
}
