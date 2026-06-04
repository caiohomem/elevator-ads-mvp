import { clearRole, clearToken, getToken } from "@/lib/auth/storage";
import type { PagedQuery, PagedResult } from "@/lib/types";

export interface ApiError {
  status: number;
  message: string;
}

export type ApiResult<T> = { ok: true; data: T } | ({ ok: false } & ApiError);
export type ApiResource<T> = { ok: true; data: T };

type ListResponse<T> = T[] | PagedResult<T>;

export async function apiFetchList<T>(path: string): Promise<ApiResult<T[]>> {
  const result = await apiFetch<ListResponse<T>>(path);

  if (!result.ok) {
    return result;
  }

  return {
    ok: true,
    data: Array.isArray(result.data) ? result.data : result.data.items,
  };
}

export async function apiFetch<T>(path: string): Promise<ApiResult<T>> {
  const requestUrl = buildRequestUrl(path);

  try {
    const response = await fetch(requestUrl, {
      headers: createHeaders(),
    });

    if (!response.ok) {
      handleAuthRedirect(response.status);
      return {
        ok: false,
        status: response.status,
        message: await readErrorMessage(response),
      };
    }

    return { ok: true, data: (await response.json()) as T };
  } catch (error) {
    return {
      ok: false,
      status: 0,
      message: error instanceof Error ? error.message : "Unable to reach the API.",
    };
  }
}

export async function apiFetchPaged<T>(
  path: string,
  query: PagedQuery,
): Promise<ApiResult<PagedResult<T>>> {
  const searchParams = new URLSearchParams();

  searchParams.set("page", String(query.page));
  searchParams.set("pageSize", String(query.pageSize));

  if (query.sortBy) {
    searchParams.set("sortBy", query.sortBy);
  }

  if (query.sortDirection) {
    searchParams.set("sortDirection", query.sortDirection);
  }

  if (query.search) {
    searchParams.set("search", query.search);
  }

  if (query.status) {
    searchParams.set("status", query.status);
  }

  const queryString = searchParams.toString();
  const requestPath = queryString ? `${path}?${queryString}` : path;

  return apiFetch<PagedResult<T>>(requestPath);
}

export async function apiMutate<TBody, TResponse>(
  path: string,
  method: "POST" | "PUT",
  body: TBody,
): Promise<ApiResult<TResponse>> {
  const requestUrl = buildRequestUrl(path);

  try {
    const response = await fetch(requestUrl, {
      method,
      headers: createHeaders(true),
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      handleAuthRedirect(response.status);
      return {
        ok: false,
        status: response.status,
        message: await readErrorMessage(response),
      };
    }

    return { ok: true, data: (await response.json()) as TResponse };
  } catch (error) {
    return {
      ok: false,
      status: 0,
      message: error instanceof Error ? error.message : "Unable to reach the API.",
    };
  }
}

export async function apiDelete(path: string): Promise<ApiResult<void>> {
  const requestUrl = buildRequestUrl(path);

  try {
    const response = await fetch(requestUrl, {
      method: "DELETE",
      headers: createHeaders(),
    });

    if (!response.ok) {
      handleAuthRedirect(response.status);
      return {
        ok: false,
        status: response.status,
        message: await readErrorMessage(response),
      };
    }

    return { ok: true, data: undefined };
  } catch (error) {
    return {
      ok: false,
      status: 0,
      message: error instanceof Error ? error.message : "Unable to reach the API.",
    };
  }
}

async function readErrorMessage(response: Response) {
  const contentType = response.headers.get("content-type") ?? "";

  if (contentType.includes("application/json")) {
    const body = (await response.json().catch(() => null)) as {
      message?: string;
      error?: string;
      title?: string;
    } | null;

    return body?.message ?? body?.error ?? body?.title ?? response.statusText;
  }

  return (await response.text().catch(() => "")) || response.statusText;
}

function createHeaders(includeJsonContentType = false): HeadersInit {
  const headers: HeadersInit = {
    Accept: "application/json",
  };

  if (includeJsonContentType) {
    headers["Content-Type"] = "application/json";
  }

  const token = getToken();

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  return headers;
}

function handleAuthRedirect(status: number) {
  if (typeof window === "undefined") {
    return;
  }

  if (status === 401) {
    clearToken();
    clearRole();
    window.location.href = "/login";
    return;
  }

  if (status === 403) {
    window.location.href = "/forbidden";
  }
}

function buildRequestUrl(path: string) {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  if (typeof window !== "undefined") {
    return path;
  }

  const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";
  return new URL(path, baseUrl).toString();
}

export function logDependentEndpointWarning(endpoint: string, error: ApiError, fallback: string) {
  console.warn(`API request to ${endpoint} failed (${error.status}): ${error.message}. Falling back to ${fallback}.`);
}

export function withMockFallback<T>(endpoint: string, error: ApiError, fallbackData: T): ApiResource<T> {
  logDependentEndpointWarning(endpoint, error, "mock fallback data");
  return {
    ok: true,
    data: fallbackData,
  };
}
