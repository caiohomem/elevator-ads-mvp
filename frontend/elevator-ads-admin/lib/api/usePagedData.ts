"use client";

import { useCallback, useEffect, useState } from "react";
import type { ApiResult } from "@/lib/api/client";
import type { PagedQuery, PagedResult } from "@/lib/types";

type AsyncPagedState<T> =
  | { status: "loading"; retry: () => void }
  | { status: "error"; message: string; retry: () => void }
  | { status: "ok"; data: PagedResult<T>; retry: () => void };

type Fetcher<T> = (query: PagedQuery) => Promise<ApiResult<PagedResult<T>>>;

const DEFAULT_PAGE_SIZE = 20;
const PAGE_SIZE_OPTIONS = [10, 20, 50] as const;
const noopRetry = () => undefined;

function readStoredPageSize(storageKey: string | undefined) {
  if (!storageKey || typeof window === "undefined") {
    return DEFAULT_PAGE_SIZE;
  }

  const storedValue = window.sessionStorage.getItem(storageKey);
  const parsedValue = storedValue ? Number(storedValue) : Number.NaN;

  return PAGE_SIZE_OPTIONS.includes(parsedValue as (typeof PAGE_SIZE_OPTIONS)[number])
    ? parsedValue
    : DEFAULT_PAGE_SIZE;
}

function writeStoredPageSize(storageKey: string | undefined, pageSize: number) {
  if (!storageKey || typeof window === "undefined") {
    return;
  }

  window.sessionStorage.setItem(storageKey, String(pageSize));
}

export function usePagedData<T>(
  fetcher: Fetcher<T>,
  storageKey?: string,
) {
  const [query, setQueryState] = useState<PagedQuery>(() => ({
    page: 1,
    pageSize: readStoredPageSize(storageKey),
  }));
  const [state, setState] = useState<AsyncPagedState<T>>({ status: "loading", retry: noopRetry });
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    writeStoredPageSize(storageKey, query.pageSize);
  }, [query.pageSize, storageKey]);

  const retry = useCallback(() => {
    setState({ status: "loading", retry: noopRetry });
    setReloadKey((key) => key + 1);
  }, []);

  const updateQuery = useCallback((next: Partial<PagedQuery> | ((current: PagedQuery) => PagedQuery)) => {
    setState({ status: "loading", retry: noopRetry });
    setQueryState((current) => {
      const updated = typeof next === "function" ? next(current) : { ...current, ...next };

      return {
        page: Math.max(1, updated.page),
        pageSize: updated.pageSize,
        sortBy: updated.sortBy,
        sortDirection: updated.sortDirection,
        search: updated.search,
        status: updated.status,
      };
    });
  }, []);

  const setPage = useCallback((page: number) => {
    updateQuery((current) => ({ ...current, page }));
  }, [updateQuery]);

  const setPageSize = useCallback((pageSize: number) => {
    updateQuery((current) => ({ ...current, page: 1, pageSize }));
  }, [updateQuery]);

  const setSearch = useCallback((search: string) => {
    updateQuery((current) => ({ ...current, page: 1, search: search || undefined }));
  }, [updateQuery]);

  const setStatus = useCallback((status?: string) => {
    updateQuery((current) => ({ ...current, page: 1, status: status || undefined }));
  }, [updateQuery]);

  const setSortBy = useCallback((sortBy?: string) => {
    updateQuery((current) => ({ ...current, page: 1, sortBy }));
  }, [updateQuery]);

  const setSortDirection = useCallback((sortDirection?: "asc" | "desc") => {
    updateQuery((current) => ({ ...current, page: 1, sortDirection }));
  }, [updateQuery]);

  useEffect(() => {
    let active = true;

    fetcher(query)
      .then((resource) => {
        if (!active) {
          return;
        }

        if (resource.ok) {
          setState({ status: "ok", data: resource.data, retry });
          return;
        }

        setState({ status: "error", message: resource.message, retry });
      })
      .catch((error) => {
        if (!active) {
          return;
        }

        setState({
          status: "error",
          message: error instanceof Error ? error.message : "Unable to load data.",
          retry,
        });
      });

    return () => {
      active = false;
    };
  }, [fetcher, query, reloadKey, retry]);

  return {
    state,
    query,
    setQuery: updateQuery,
    setPage,
    setPageSize,
    setSearch,
    setStatus,
    setSortBy,
    setSortDirection,
  };
}
