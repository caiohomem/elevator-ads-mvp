"use client";

import { useCallback, useEffect, useState } from "react";
import type { ApiResult } from "@/lib/api/client";

type AsyncState<T> =
  | { status: "loading"; retry: () => void }
  | { status: "error"; message: string; retry: () => void }
  | { status: "ok"; data: T; retry: () => void };

type AsyncStateWithoutRetry<T> =
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "ok"; data: T };

export function useApiData<T>(fetcher: () => Promise<ApiResult<T>>): AsyncState<T> {
  const [state, setState] = useState<AsyncStateWithoutRetry<T>>({ status: "loading" });
  const [reloadKey, setReloadKey] = useState(0);

  const retry = useCallback(() => {
    setState({ status: "loading" });
    setReloadKey((key) => key + 1);
  }, []);

  useEffect(() => {
    let active = true;

    fetcher()
      .then((resource) => {
        if (!active) {
          return;
        }

        if (resource.ok) {
          setState({ status: "ok", data: resource.data });
          return;
        }

        setState({ status: "error", message: resource.message });
      })
      .catch((error) => {
        if (!active) {
          return;
        }

        setState({
          status: "error",
          message: error instanceof Error ? error.message : "Unable to load data.",
        });
      });

    return () => {
      active = false;
    };
  }, [fetcher, reloadKey]);

  return { ...state, retry } as AsyncState<T>;
}
