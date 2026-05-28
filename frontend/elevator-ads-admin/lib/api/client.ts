export interface ApiError {
  status: number;
  message: string;
}

export type ApiResult<T> = { ok: true; data: T } | ({ ok: false } & ApiError);

export async function apiFetch<T>(path: string): Promise<ApiResult<T>> {
  const requestUrl = buildRequestUrl(path);

  try {
    const response = await fetch(requestUrl, {
      headers: {
        Accept: "application/json",
      },
    });

    if (!response.ok) {
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
